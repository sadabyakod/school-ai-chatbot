using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageAPI.TestImageGenerator
{
    public class TestImageGenerator
    {
        public static void GenerateTestImages(string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Generate test image 1 - Red rectangle
            GenerateColoredRectangle(Path.Combine(outputDirectory, "test-red.png"), 
                                   Color.Red, 200, 150, "Test Red Image");

            // Generate test image 2 - Blue circle
            GenerateCircle(Path.Combine(outputDirectory, "test-blue.png"), 
                          Color.Blue, 200, 200, "Test Blue Circle");

            // Generate test image 3 - Green gradient
            GenerateGradient(Path.Combine(outputDirectory, "test-gradient.jpg"), 
                           Color.Green, Color.LightGreen, 300, 200);

            Console.WriteLine($"Generated test images in: {outputDirectory}");
        }

        private static void GenerateColoredRectangle(string filePath, Color color, int width, int height, string text)
        {
            using (var bitmap = new Bitmap(width, height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(color);
                
                // Add text
                using (var font = new Font("Arial", 12, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var textSize = graphics.MeasureString(text, font);
                    graphics.DrawString(text, font, brush, 
                                      (width - textSize.Width) / 2, 
                                      (height - textSize.Height) / 2);
                }

                bitmap.Save(filePath, ImageFormat.Png);
            }
        }

        private static void GenerateCircle(string filePath, Color color, int width, int height, string text)
        {
            using (var bitmap = new Bitmap(width, height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw circle
                using (var brush = new SolidBrush(color))
                {
                    var diameter = Math.Min(width, height) - 20;
                    var x = (width - diameter) / 2;
                    var y = (height - diameter) / 2;
                    graphics.FillEllipse(brush, x, y, diameter, diameter);
                }

                // Add text
                using (var font = new Font("Arial", 10, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var textSize = graphics.MeasureString(text, font);
                    graphics.DrawString(text, font, brush, 
                                      (width - textSize.Width) / 2, 
                                      (height - textSize.Height) / 2);
                }

                bitmap.Save(filePath, ImageFormat.Png);
            }
        }

        private static void GenerateGradient(string filePath, Color startColor, Color endColor, int width, int height)
        {
            using (var bitmap = new Bitmap(width, height))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, width, height), startColor, endColor, 45f))
                {
                    graphics.FillRectangle(brush, 0, 0, width, height);
                }

                // Add text
                using (var font = new Font("Arial", 12, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Black))
                {
                    var text = "Gradient Test";
                    var textSize = graphics.MeasureString(text, font);
                    graphics.DrawString(text, font, brush, 
                                      (width - textSize.Width) / 2, 
                                      (height - textSize.Height) / 2);
                }

                bitmap.Save(filePath, ImageFormat.Jpeg);
            }
        }
    }
}