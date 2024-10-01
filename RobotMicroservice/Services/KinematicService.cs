using RobotMicroservice.Utils;

namespace RobotMicroservice.Services
{
    // All the code here come from a repo github : 
    // https://github.com/glumb/kinematics/blob/master/src/kinematics.js
    // it's just a c# rewrite
    public class KinematicService
    {

        private double V1_length_x_y;
        private double V4_length_x_y_z;
        private double[][] geometry;
        private double[] R_corrected;
        private List<double[]> J_initial_absolute;
        private bool debug = false;

        public void SetRobotConfiguration(double[][] configuration){

            this.geometry = configuration;
            if (geometry.Length != 5)
            {
                throw new Exception("geometry array must have 5 entries");
            }

            if (geometry[3][1] != 0 || geometry[3][2] != 0 || geometry[4][0] != 0 || geometry[4][2] != 0)
            {
                throw new Exception("geometry 3 and 4 must be one dimensional geo[3] = [a,0,0] geo[4] = [0,b,0]");
            }

            this.V1_length_x_y = Math.Sqrt(Math.Pow(geometry[1][0], 2) + Math.Pow(geometry[1][1], 2));
            this.V4_length_x_y_z = Math.Sqrt(Math.Pow(geometry[4][0], 2) + Math.Pow(geometry[4][1], 2) + Math.Pow(geometry[4][2], 2));

            this.J_initial_absolute = new List<double[]>();
            double[] tmpPos = { 0, 0, 0 };
            for (int i = 0; i < geometry.Length; i++)
            {
                this.J_initial_absolute.Add(new double[] { tmpPos[0], tmpPos[1], tmpPos[2] });
                tmpPos[0] += geometry[i][0];
                tmpPos[1] += geometry[i][1];
                tmpPos[2] += geometry[i][2];
            }

            this.R_corrected = new double[6];
            this.R_corrected[1] -= Math.PI / 2;
            this.R_corrected[1] += Math.Atan2(geometry[1][0], geometry[1][1]); // correct offset bone

            this.R_corrected[2] -= Math.PI / 2;
            this.R_corrected[2] -= Math.Atan2((geometry[2][1] + geometry[3][1]), (geometry[2][0] + geometry[3][0])); // correct offset bone V2,V3
            this.R_corrected[2] -= Math.Atan2(geometry[1][0], geometry[1][1]); // correct bone offset of V1

            this.R_corrected[4] += Math.Atan2(geometry[4][1], geometry[4][0]);

            DisplayGeometry();
        }

        public double[] inverse(double x, double y, double z, double a, double b, double c)
        {
            double ca = Math.Cos(a);
            double sa = Math.Sin(a);
            double cb = Math.Cos(b);
            double sb = Math.Sin(b);
            double cc = Math.Cos(c);
            double sc = Math.Sin(c);

            double[] targetVectorX = { cb * cc, cb * sc, -sb };

            double[] R = {
                this.R_corrected[0],
                this.R_corrected[1],
                this.R_corrected[2],
                this.R_corrected[3],
                this.R_corrected[4],
                this.R_corrected[5]
            };

            double[][] J = {
                new double[3],
                new double[3],
                new double[3],
                new double[3],
                new double[3],
                new double[3]
            };

            // ---- J5 ----
            J[5][0] = x;
            J[5][1] = y;
            J[5][2] = z;

            // ---- J4 ----
            // vector

            J[4][0] = x - this.V4_length_x_y_z * targetVectorX[0];
            J[4][1] = y - this.V4_length_x_y_z * targetVectorX[1];
            J[4][2] = z - this.V4_length_x_y_z * targetVectorX[2];

            // ---- R0 ----
            // # J4
            R[0] += Math.PI / 2 - Math.Acos(this.J_initial_absolute[4][2] / KinematicUtils.Length2(J[4][2], J[4][0]));
            R[0] += Math.Atan2(-J[4][2], J[4][0]);

            if (this.J_initial_absolute[4][2] > KinematicUtils.Length2(J[4][2], J[4][0]) && this.debug)
            {
                Console.WriteLine("out of reach");
            }

            // ---- J1 ----
            J[1][0] = Math.Cos(R[0]) * this.geometry[0][0] + Math.Sin(R[0]) * this.geometry[0][2];
            J[1][1] = this.geometry[0][1];
            J[1][2] = -Math.Sin(R[0]) * this.geometry[0][0] + Math.Cos(R[0]) * this.geometry[0][2];

            // ---- rotate J4 into x,y plane ----
            double[] J4_x_y = new double[3];
            J4_x_y[0] = Math.Cos(R[0]) * J[4][0] - Math.Sin(R[0]) * J[4][2];
            J4_x_y[1] = J[4][1];
            J4_x_y[2] = Math.Sin(R[0]) * J[4][0] + Math.Cos(R[0]) * J[4][2];

            // ---- J1J4_projected_length_square ----
            // # J4 R0

            double J1J4_projected_length_square = Math.Pow(J4_x_y[0] - this.J_initial_absolute[1][0], 2) +
                                                  Math.Pow(J4_x_y[1] - this.J_initial_absolute[1][1], 2);

            // ---- R2 ----
            // # J4 R0

            double J2J4_length_x_y = KinematicUtils.Length2(this.geometry[2][0] + this.geometry[3][0], this.geometry[2][1] + this.geometry[3][1]);
            R[2] += Math.Acos((-J1J4_projected_length_square + Math.Pow(J2J4_length_x_y, 2) + Math.Pow(this.V1_length_x_y, 2)) /
                              (2.0 * J2J4_length_x_y * this.V1_length_x_y));

            // ---- R1 ----
            // # J4 R0

            double J1J4_projected_length = Math.Sqrt(J1J4_projected_length_square);
            R[1] += Math.Atan2(J4_x_y[1] - this.J_initial_absolute[1][1], J4_x_y[0] - this.J_initial_absolute[1][0]);
            R[1] += Math.Acos((J1J4_projected_length_square - Math.Pow(J2J4_length_x_y, 2) + Math.Pow(this.V1_length_x_y, 2)) /
                              (2.0 * J1J4_projected_length * this.V1_length_x_y));

            // ---- J2 ----
            // # R1 R0
            double ta = Math.Cos(R[0]);
            double tb = Math.Sin(R[0]);
            double tc = this.geometry[0][0];
            double d = this.geometry[0][1];
            double e = this.geometry[0][2];
            double f = Math.Cos(R[1]);
            double g = Math.Sin(R[1]);
            double h = this.geometry[1][0];
            double i = this.geometry[1][1];
            double j = this.geometry[1][2];
            double k = Math.Cos(R[2]);
            double l = Math.Sin(R[2]);
            double m = this.geometry[2][0];
            double n = this.geometry[2][1];
            double o = this.geometry[2][2];

            J[2][0] = ta * tc + tb * e + ta * f * h - ta * g * i + tb * j;
            J[2][1] = d + g * h + f * i;
            J[2][2] = -tb * tc + ta * e - tb * f * h + tb * g * i + ta * j;

            // ---- J3 ----
            // # R0, R1, R2

            J[3][0] = ta * tc + tb * e + ta * f * h - ta * g * i + tb * j + ta * f * k * m - ta * g * l * m - ta * g * k * n - ta * f * l * n + tb * o;
            J[3][1] = d + g * h + f * i + g * k * m + f * l * m + f * k * n - g * l * n;
            J[3][2] = -tb * tc + ta * e - tb * f * h + tb * g * i + ta * j - tb * f * k * m + tb * g * l * m + tb * g * k * n + tb * f * l * n + ta * o;

            // ---- J4J3, J4J5 ----
            // # J3, J4, J5

            double[] J4J5_vector = { J[5][0] - J[4][0], J[5][1] - J[4][1], J[5][2] - J[4][2] };
            double[] J4J3_vector = { J[3][0] - J[4][0], J[3][1] - J[4][1], J[3][2] - J[4][2] };

            // ---- R3 ----
            // # J3, J4, J5

            double[] J4J5_J4J3_normal_vector = KinematicUtils.Cross(J4J5_vector, J4J3_vector);
            double[] XZ_parallel_aligned_vector = {
                10 * Math.Cos(R[0] + (Math.PI / 2)),
                0,
                -10 * Math.Sin(R[0] + (Math.PI / 2))
            };

            double[] reference = KinematicUtils.Cross(XZ_parallel_aligned_vector, J4J3_vector);
            R[3] = KinematicUtils.AngleBetween(J4J5_J4J3_normal_vector, XZ_parallel_aligned_vector, reference);

            // ---- R4 ----
            // # J4, J3, J5

            double[] referenceVector = KinematicUtils.Cross(J4J3_vector, J4J5_J4J3_normal_vector);
            R[4] += KinematicUtils.AngleBetween(J4J5_vector, J4J3_vector, referenceVector);

            // ---- R5 ----
            // # J3, J4, J5

            double[] targetVectorY = {
                sa * sb * cc - sc * ca,
                sa * sb * sc + cc * ca,
                sa * cb
            };

            R[5] += Math.PI / 2;
            R[5] -= KinematicUtils.AngleBetween(J4J5_J4J3_normal_vector, targetVectorY, KinematicUtils.Cross(targetVectorY, targetVectorX));


            return R;
        }

        private void DisplayGeometry()
        {
            Console.WriteLine("Robot is configured with the following geometry:");

            // Check if the geometry is not null
            if (this.geometry != null)
            {
                for (int i = 0; i < this.geometry.Length; i++)
                {
                    // Print each row of the jagged array
                    Console.Write($"Row {i}: ");
                    for (int j = 0; j < this.geometry[i].Length; j++)
                    {
                        Console.Write(this.geometry[i][j] + " ");
                    }
                    Console.WriteLine(); // Move to the next line after printing a row
                }
            }
            else
            {
                Console.WriteLine("Geometry is not configured.");
            }
        }

    }
}
