using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Project_DICOM
{
    public partial class MainWindow : Window
    {
        string filename = @"D:\infa_studia\10semestr\WZI\sikor\Wybrane_zastosowania_informatyki\head-dicom\IM2";
        byte[] bytes;
        byte[] bitMap1;
        byte[] bitMap2;
        byte[] bitMap3;
        byte[][][]pixels;
        string type = "";
        int width = 512, height = 512;
        int countFiles = 1;

        List<string> specialTags = new List<string>() { "OB", "OW", "OF", "SQ", "UT", "UN" };
        NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;

        // (7fe0, 0010) Pixel Data
        int[][] pixelData;

        // (0028, 0010) Rows
        ushort rows;

        // (0028, 0011) Columns
        public ushort cols;

        // (0028, 0101) Bits Stored
        ushort bitsStored;

        // (0028, 0100) Bits Allocated
        ushort bitsAllocated;

        // (0028, 1053) Rescale Slope
        float rescaleSlope;

        // (0028, 1052) Rescale Intercept
        float rescaleIntercept;

        // (0028, 0030) Pixel Spacing [2]
        float[] pixelSpacing = new float[2];

        //(0018, 0050) Slice Thickness
        float sliceThickness;

        // (0020, 0032) Image Position (Patient) [3]
        float[] imagePosition = new float[3];

        // (0028, 1050) Window Center
        float windowCenter;

        // (0028, 1051) Window Width
        float windowWidth;

        // (0020, 1041) Slice Location
        float sliceLocation;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void LoadFiles()
        {
            //bytes = File.ReadAllBytes(filename);
            bool found = false;
            uint i;

            // Search for DICM
            for (i = 0; i + 3 < bytes.Length; i += 4)
            {
                if (bytes[i] == 'D' && bytes[i + 1] == 'I' && bytes[i + 2] == 'C' && bytes[i + 3] == 'M')
                {
                    Debug.WriteLine("DICM found");
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Debug.WriteLine("DICM not found");
                return;
            }

            uint skip = 0;
            uint count = 0;
            ushort tagGroup;
            ushort tagNumber;

            for (i += 4; i < bytes.Length; i += skip)
            {
                skip = 6;
                tagGroup = (ushort)(((0xff & bytes[i + 1]) << 8) + (0xff & bytes[i]));
                tagNumber = (ushort)((bytes[i + 3] << 8) + bytes[i + 2]);

                string dataType = "";
                dataType += (char)bytes[i + 4];
                dataType += (char)bytes[i + 5];

                if (specialTags.Contains(dataType))
                {
                    count = (uint)((bytes[i + 11] << 24) + (bytes[i + 10] << 16) + (bytes[i + 9] << 8) + bytes[i + 8]);
                    skip += 4;
                }
                else
                {
                    count = (uint)((bytes[i + 7] << 8) + bytes[i + 6]);
                }
                skip += 2 + count;
               
                // (0028, 0010) Rows
                if (tagGroup == 0x0028 && tagNumber == 0x0010) 
                {
                    rows = (ushort) ((bytes[i + skip - count + 1] << 8) + (bytes[i + skip - count]));
                }

                // (0028, 0011) Columns
                if (tagGroup == 0x0028 && tagNumber == 0x0011) 
                {
                    cols = (ushort) ((bytes[i + skip - count + 1] << 8) + (bytes[i + skip - count]));
                }

                // (0028, 0101) Bits Stored
                if (tagGroup == 0x0028 && tagNumber == 0x0101) 
                {
                    bitsStored = (ushort) ((bytes[i + skip - count + 1] << 8) + (bytes[i + skip - count]));
                }

                // (0028, 0100) Bits Allocated
                if (tagGroup == 0x0028 && tagNumber == 0x0100) 
                {
                    bitsAllocated = (ushort) ((bytes[i + skip - count + 1] << 8) + (bytes[i + skip - count]));
                }
                // (0028, 1053) Rescale Slope
                if (tagGroup == 0x0028 && tagNumber == 0x1053) 
                {
                    string buffer = CheckPattern(i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);
 
                    rescaleSlope = a;
                }
                
                // (0028, 1052) Rescale Intercept
                if (tagGroup == 0x0028 && tagNumber == 0x1052) 
                {
                    string buffer = CheckPattern(i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    rescaleIntercept = a;
                }
                
                // (0028, 0030) Pixel Spacing [2]
                if (tagGroup == 0x0028 && tagNumber == 0x0030) 
                {
                    string buffer = CheckPattern(i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);
                    float b = float.Parse(parts[1], nfi);

                    pixelSpacing[0] = a;
                    pixelSpacing[1] = b;
                }

                //(0018, 0050) Slice Thickness
                if (tagGroup == 0x0018 && tagNumber == 0x0050)
                {
                    string buffer = CheckPattern(i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(buffer, nfi);

                    sliceThickness = a;
                }
                
                // (0020, 0032) Image Position (Patient) [3]
                if (tagGroup == 0x0020 && tagNumber == 0x0032) 
                {
                    string buffer = CheckPattern(i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    
                    float a = float.Parse(parts[0], nfi);
                    float b = float.Parse(parts[1], nfi);
                    float c = float.Parse(parts[2], nfi);

                    imagePosition[0] = a;
                    imagePosition[1] = b;
                    imagePosition[2] = c;
                }

                // (0028, 1050) Window Center
                if (tagGroup == 0x0028 && tagNumber == 0x1050) 
                {
                    string buffer = CheckPattern(i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    windowCenter = a;
                }

                // (0028, 1051) Window Width
                if (tagGroup == 0x0028 && tagNumber == 0x1051) 
                {
                    string buffer = CheckPattern(i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    windowWidth = a;
                }

                // (0020, 1041) Slice Location
                if (tagGroup == 0x0020 && tagNumber == 0x1041) 
                {
                    string buffer = CheckPattern(i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    sliceLocation = a;
                }

                // (7fe0, 0010) Pixel Data
                if (tagGroup == 0x7fe0 && tagNumber == 0x0010) 
                {
                    i += skip - count;
                    break;
                }
            }

            // Read Pixels
            pixelData = new int[rows][];
            for (uint k = 0; k < rows; k++)
            {
                pixelData[k] = new int[cols];
            }
            uint row = 0, col = 0;
            for (; i < bytes.Length; i += 2)
            {
                pixelData[row][col] = ((0xff & bytes[i + 1]) << 8) + (0xff & bytes[i]);
                col++;
                if (col == cols)
                {
                    col = 0;
                    row++;
                }
            }
            SetSliders();
        }

        public void SetSliders()
        {
            slider1.Maximum = countFiles - 1;
            slider2.Maximum = rows - 1;
            slider3.Maximum = cols - 1;

            slider1.Value = 0;
            slider2.Value = 0;
            slider3.Value = 0;
        }

        public string CheckPattern(uint start, uint end)
        {
            char[] buffer = new char[(ulong) (end - start + 1)];
            int i = 0;
            for (; bytes[start] != bytes[end]; start++)
            {
                buffer[i] = (char)bytes[start];
                i++;
            }
            buffer[i] = '\0';
            return new string(buffer);
        }

        public byte GetColor(int x, int y, string type)
        {
            float value = pixelData[x][y] * rescaleSlope + rescaleIntercept;
            byte yMin = 0;
            byte yMax = 255;
            float center = windowCenter - 0.5f;
            float width = windowWidth - 1.0f;
            if (type == "bone")
            {
                // 300-400 i 500-1900
                center = 300 - 0.5f;
                width = 1900 - 1.0f;
            }
            if (type == "brain")
            {
                // 40-80
                center = 40 - 0.5f;
                width = 80 - 1.0f;
            }
            if (type == "angio")
            {
                // 300-600
                center = 300 - 0.5f;
                width = 600 - 1.0f;
            }

            if (value <= (center - width / 2.0f))
            {
                return yMin;
            }
            else if (value > (center + width / 2.0f))
            {
                return yMax;
            }
            else
            {
                return (byte)(((value - center) / width + 0.5f) * (yMax - yMin) + yMin);
            }
        }

        public void DrawImage1()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            bitMap1 = new byte[stride * height];
            FillPixels();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    SetBitMap(bitMap1, i, j, pixels[(int)slider1.Value][i][j]);
                }
            }

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap1, stride);
            //var bitmap = new TransformedBitmap(bs, 
            //    new ScaleTransform(1, 1));
            Image1.Source = bs;
        }
        public void DrawImage2()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            bitMap2 = new byte[stride * height];
            FillPixels();
            for (int i = 0; i < countFiles; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    SetBitMap(bitMap2, countFiles - 1 - i, j, pixels[i][(int)slider2.Value][j]);
                }
            }
            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap2, stride);
            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap, 
                new ScaleTransform(1, countFiles * sliceThickness / pixelSpacing[0]));
            Image2.Source = bitmap;
        }
        public void DrawImage3()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            bitMap3 = new byte[stride * height];
            FillPixels();
            for (int i = 0; i < countFiles; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    SetBitMap(bitMap3, countFiles - 1 - i, j, pixels[i][j][(int)slider3.Value]);
                }
            }

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap3, stride);
            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap,
                new ScaleTransform(1, countFiles * sliceThickness / pixelSpacing[1]));
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
            pixels = new byte[1][][];
            for (int i = 0; i < countFiles; i++) //na razie dla 1 obrazka
            {
                pixels[i] = new byte[rows][];
                for (int j = 0; j < rows; j++)
                {
                    pixels[i][j] = new byte[cols];
                    for (int k = 0; k < rows; k++)
                    {
                        pixels[i][j][k] = GetColor(j, k, type);
                    }
                }
            }
        }
        public void SetBitMap(byte[] bitMap, int x, int y, byte color)
        {
            bitMap[512 * 4 * x + 4 * y] = color;
            bitMap[512 * 4 * x + 4 * y + 1] = color;
            bitMap[512 * 4 * x + 4 * y + 2] = color;
        }

        private void OnSlide1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DrawImage1();
        }

        private void OnSlide2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DrawImage2();
        }

        private void OnSlide3(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DrawImage3();
        }

        private void OnPickFolder(object sender, RoutedEventArgs e)
        {
            string directory = "";
            var fbd = new FolderBrowserDialog();
            byte[] fileBytes = null;
            fbd.SelectedPath = @"D:\infa_studia\10semestr\WZI\sikor\Wybrane_zastosowania_informatyki\";
            DialogResult result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                directory = fbd.SelectedPath;
            }

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                fileBytes = File.ReadAllBytes(file);
            }
            bytes = fileBytes;
            LoadFiles();
            DrawImages();

        }

        public void ClearBitMaps()
        {
            for (int i = 0; i < 512; i++)
            {
                for (int j = 0; j < 512; j++)
                {
                    SetBitMap(bitMap1, i, j, 0);
                    SetBitMap(bitMap2, i, j, 0);
                    SetBitMap(bitMap3, i, j, 0);
                }
            }
        }
    }
}
