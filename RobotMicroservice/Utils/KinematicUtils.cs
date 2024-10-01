using System.Numerics;

namespace RobotMicroservice.Utils
{
    public class KinematicUtils
    {
        public static double Length2(double a, double b)
        {
            return Math.Sqrt((a*a) + (b *b));
        }

        public static double Length3(double[] vector)
        {
            return Math.Sqrt((vector[0] * vector[0]) + (vector[1] * vector[1]) + (vector[2]* vector[2]));
        }

        public static double Dot(double[] vectorA, double[] vectorB)
        {
            return vectorA[0] * vectorB[0] + vectorA[1] * vectorB[1] + vectorA[2] * vectorB[2];
        }

        public static double[] Cross(double[] vectorA, double[] vectorB)
        {
            return new double[] {
                vectorA[1] * vectorB[2] - vectorA[2] * vectorB[1],
                vectorA[2] * vectorB[0] - vectorA[0] * vectorB[2],
                vectorA[0] * vectorB[1] - vectorA[1] * vectorB[0]
            };
        }

        public static double AngleBetween(double[] vectorA, double[] vectorB, double[] referenceVector)
        {
            double norm = Length3(Cross(vectorA, vectorB));
            double angle = Math.Atan2(norm, Dot(vectorA, vectorB));

            double tmp = referenceVector[0] * vectorA[0] + referenceVector[1] * vectorA[1] + referenceVector[2] * vectorA[2];

            double sign = 1;
            if (tmp < 0)
            {
                sign = -sign;
            }

            return angle*sign;
        }

        public static double AngleBetween2(double[] v1, double[] v2)
        {
            double[] crossProduct = Cross(v1, v2);
            double normCross = Length3(crossProduct);

            return Math.Atan2(normCross, Dot(v1, v2));
        }
    }
}
