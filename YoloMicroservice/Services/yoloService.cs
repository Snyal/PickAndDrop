using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Advanced;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Flann;
using RobotManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Drawing;

namespace RobotManagementSystem.Services
{
    public class YoloService
    {
        private InferenceSession? _session;
        private NodeMetadata? _metadataModel;
        private readonly float threadhold = 0.5f;

        public YoloService()
        {
            SetInferenceSession("yolov8n");
        }

        public void Dispose()
        {
            _session?.Dispose();
        }

        public void SetInferenceSession(string name)
        {
            // Load the YOLO model from the ONNX file
            _session = new InferenceSession($"Yolov8/{name}.onnx");
            _metadataModel = _session.InputMetadata["images"];

            Console.WriteLine($"Shape: {string.Join(", ", _metadataModel.Dimensions)}");
            Console.WriteLine($"Data Type: {_metadataModel.ElementType}");
            Console.WriteLine($"Description:");
            Console.WriteLine(new string('-', 20));
        }

        public List<DetectionResult> DetectObjects(Stream imageStream)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Load the image using ImageSharp
            using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imageStream);
            float imageWidth = image.Width;
            float imageHeight = image.Height; 

            var input = PreprocessImage(image);

            // Create the inputs for inference
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("images", input)
            };

            using var results =  _session!.Run(inputs);

            var detectionResults = PostProcessResults(results, imageWidth, imageHeight);


            Console.WriteLine($"Time to do inference: {stopwatch.Elapsed}");
            return detectionResults;
        }

        private Tensor<float> PreprocessImage(Image<Rgb24> image)
        {
            int nbChanel = _metadataModel!.Dimensions[1];
            int imgDim = _metadataModel.Dimensions[2];

            // Resize image
            image.Mutate(x =>
            {
                x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(imgDim, imgDim),
                    Mode = ResizeMode.Pad
                });
            });

            var inputDimensions = new int[] { 1, nbChanel, imgDim, imgDim };

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var tensor = ImageToTensor(image, inputDimensions);

            Console.WriteLine($"Time to create batches: {stopwatch.Elapsed}");
            Console.WriteLine();

            return tensor;
        }

        private List<DetectionResult> PostProcessResults(IEnumerable<DisposableNamedOnnxValue> results, float baseWidthImage, float baseHeightImage)
        {
            var detections = new List<DetectionResult>();

            var output = results.FirstOrDefault(r => r.Name == "output0");
            if (output == null)
            {
                return [];
            }

            // outputTensor shape representing [xc,yy,w,h, class_1_conf, class_2_conf,...,class_N_conf, * N]
            var outputTensor = output.AsTensor<float>();

            for (int i = 0; i < outputTensor.Dimensions[2]; i++)
            {

                (int classId, float confidence) = GetClassAndConfidence(outputTensor, i);
                if (confidence > threadhold)
                {
                    float xCenter = (outputTensor[0, 0, i]);
                    float yCenter = (outputTensor[0, 1, i]);
                    float width = (outputTensor[0, 2, i]);
                    float height = (outputTensor[0, 3, i]);

                    float scale = Math.Min(_metadataModel!.Dimensions[2] / baseWidthImage, _metadataModel.Dimensions[3] / baseHeightImage);

                    // Compute the amount of padding added to the image
                    float paddingX = (_metadataModel.Dimensions[2] - (baseWidthImage * scale)) / 2.0f;
                    float paddingY = (_metadataModel.Dimensions[3] - (baseHeightImage * scale)) / 2.0f;

                    // Adjust the bounding box center and size from the padded/letterbox image to the original image size
                    float adjustedXCenter = (xCenter - paddingX) / scale;
                    float adjustedYCenter = (yCenter - paddingY) / scale;
                    float adjustedWidth = width / scale;
                    float adjustedHeight = height / scale;

                    detections.Add(new DetectionResult
                    {
                        ObjectName = classId.ToString(),
                        Confidence = confidence,
                        X = adjustedXCenter,
                        Y = adjustedYCenter,
                        Width = adjustedWidth,
                        Height = adjustedHeight
                    });

                }
            }

            Console.WriteLine($"before nms {detections.Count}");
            detections = NMS(detections);
            Console.WriteLine($"after nms {detections.Count}");

            return detections;
        }

        private static (int, float) GetClassAndConfidence(Tensor<float> outputTensor, int detectionIndex)
        {
            const int startIndex = 4;
            int classCount = outputTensor.Dimensions[1] - startIndex;
            int currentIndex = startIndex; // Start at index 4 (first class confidence)
            float maxConfidence = outputTensor[0, currentIndex, detectionIndex];  // Initial max confidence value


            // Iterate through the rest of the confidences
            for (int i = startIndex + 1; i < classCount; i++)
            {
                float confidenceToCompare = outputTensor[0, i, detectionIndex];
                if (maxConfidence < confidenceToCompare)
                {
                    maxConfidence = confidenceToCompare;
                    currentIndex = i;
                }
            }

            int finalIndex = currentIndex - startIndex;
            return (finalIndex, maxConfidence);
        }

        private static DenseTensor<float> ImageToTensor(Image<Rgb24> image, int[] inputDimension)
        {
            DenseTensor<float> input = new(inputDimension);

            using (var img = image.CloneAs<Rgb24>())
            {
                Parallel.For(0, img.Height, y => {
                    var pixelSpan = img.DangerousGetPixelRowMemory((int)y).Span;
                    for (int x = 0; x < img.Width; x++)
                    {
                        input[0, 0, y, x] = pixelSpan[x].R / 255.0F; // r
                        input[0, 1, y, x] = pixelSpan[x].G / 255.0F; // g
                        input[0, 2, y, x] = pixelSpan[x].B / 255.0F; // b
                    }
                });
            }
            return input;
        }

        private List<DetectionResult> NMS(List<DetectionResult> detections)
        {
            // Prepare for NMS
            var boxes = detections.Select(d => 
                new Rect(
                    (int)(d.X),
                    (int)(d.Y),
                    (int)(d.Width),
                    (int)(d.Height)
                )
            ).ToArray();

            var scores = detections.Select(d => d.Confidence).ToArray();
            int[] indices = [];

            float nmsThreshold = 0.4f;
            CvDnn.NMSBoxes(boxes, scores, threadhold, nmsThreshold, out indices);

            var filteredDetections = new List<DetectionResult>();
            foreach (var index in indices)
            {
                filteredDetections.Add(detections[index]);
            }

            return filteredDetections;
        }

        public Dictionary<string, string> GetModelInformation()
        {
            Dictionary<string, string> modelInformtions = new Dictionary<string, string>();

            modelInformtions.Add("Name", "yolov8m");
            modelInformtions.Add("Input_Shape", _metadataModel.Dimensions.ToString());
            modelInformtions.Add("Data_Type", _metadataModel.ElementType.ToString());
            modelInformtions.Add("Description", "paint.exe");

            return modelInformtions;
        }

    }
}
