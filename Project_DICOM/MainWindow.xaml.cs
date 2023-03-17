using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Project_DICOM
{
    public partial class MainWindow : Window
    {
        Dicom[] dicoms;
        byte[] bitMap1;
        byte[] bitMap2;
        byte[] bitMap3;
        byte[] pixels;
        int width = 512, height = 512;
        int countFiles = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetSliders()
        {
            slider1.Maximum = countFiles - 1;
            slider2.Maximum = width - 1;
            slider3.Maximum = height - 1;

            slider1.Value = 0;
            slider2.Value = 0;
            slider3.Value = 0;
        }

        public void DrawImage1()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            int sliderValue = (int)slider1.Value;
            bitMap1 = new byte[stride * height];
            FillPixels();

            Parallel.For(0, width, i =>
            {
                for (int j = 0; j < height; j++)
                {
                    SetBitMap(bitMap1, i, j, pixels[sliderValue * width * height + i * height + j]);
                }
            });

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap1, stride);
            //RenderOptions.SetBitmapScalingMode(bs, BitmapScalingMode.HighQuality);
            //var bitmap = new TransformedBitmap(bs, 
            //    new ScaleTransform(1, 1));
            Image1.Source = bs;
        }
        public void DrawImage2()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            int sliderValue = (int)slider2.Value;
            bitMap2 = new byte[stride * height];
            FillPixels();
            
            Parallel.For(0, countFiles, i =>
            {
                for (int j = 0; j < height; j++)
                {
                    SetBitMap(bitMap2, countFiles - 1 - i, j, pixels[i * width * height + sliderValue * height + j]);
                }
            });
            BitmapSource bs = BitmapSource.Create(width, countFiles, 96d, 96d, pf, null, bitMap2, stride);

            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap,
                new ScaleTransform(1, 512.0 / countFiles));
            //RenderOptions.SetBitmapScalingMode(bitmap, BitmapScalingMode.HighQuality);
            Image2.Source = bitmap;
        }

        public void DrawImage3()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            int sliderValue = (int)slider3.Value;
            bitMap3 = new byte[stride * height];
            FillPixels();

            Parallel.For(0, countFiles, i =>
            {
                for (int j = 0; j < width; j++)
                {
                    SetBitMap(bitMap3, countFiles - 1 - i, j, pixels[i * width * height + j * height + sliderValue]);
                }
            });

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap3, stride);
            
            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap,
                new ScaleTransform(1, 512.0 / countFiles));
            //RenderOptions.SetBitmapScalingMode(bitmap, BitmapScalingMode.HighQuality);
            Image3.Source = bitmap;
        }
        public void DrawImages()
        {
            DrawImage1();
            DrawImage2();
            DrawImage3();
        }
        public void FillPixels()
        {
            pixels = new byte[countFiles * width * height];
            int sliderL = (int)sliderLevel.Value;
            int sliderW = (int)sliderWidth.Value;

            Parallel.For(0, countFiles, i =>
            {
                for (int j = 0; j < width; j++)
                {
                    Parallel.For(0, height, k =>
                    {
                        pixels[i * width * height + j * height + k] = dicoms[i].GetColor(j, k, sliderL, sliderW);
                    });
                }
            });
        }

        public void SetBitMap(byte[] bitMap, int x, int y, byte color)
        {
            bitMap[512 * 4 * x + 4 * y] = color;
            bitMap[512 * 4 * x + 4 * y + 1] = color;
            bitMap[512 * 4 * x + 4 * y + 2] = color;
        }

        private void OnSlide1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(countFiles > 0)
            {
                DrawImage1();
            }
        }

        private void OnSlide2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                DrawImage2();
            }
        }

        private void OnSlide3(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                DrawImage3();
            }
        }

        private void OnSlideLevel(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                FillPixels();
                DrawImages();
            }
        }

        private void OnSlideWidth(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                FillPixels();
                DrawImages();
            }
        }

        private void OnPickFolder(object sender, RoutedEventArgs e)
        {
            string directory = "";
            var fbd = new FolderBrowserDialog();
            byte[] fileBytes;
            fbd.SelectedPath = @"D:\infa_studia\10semestr\WZI\head-dicom\";
            DialogResult result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                directory = fbd.SelectedPath;
            }
            else
            {
                return;
            }

            IEnumerable<string> files = Directory.EnumerateFiles(directory);
            List<string> lof = new List<string>(files);

            // Sortowanie plików według liczb na końcu ścieżki
            lof.Sort((s1, s2) => Int32.Parse(Regex.Match(s1, @"(\d+)(?!.*\d)").Value).
                CompareTo(Int32.Parse(Regex.Match(s2, @"(\d+)(?!.*\d)").Value)));

            countFiles = 0;
            dicoms = new Dicom[files.Count()];

            foreach (string file in lof)
            {
                fileBytes = File.ReadAllBytes(file);
                Dicom dicom = new Dicom();
                
                dicoms[countFiles] = dicom;
                dicoms[countFiles].LoadFile(fileBytes);
                countFiles += 1;
            }
            
            DrawImages();
            SetSliders();
        }
    }
}
