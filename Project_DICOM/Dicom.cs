using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Project_DICOM
{
    public class Dicom
    {
        public Dicom() { }

        List<string> specialTags = new List<string>() { "OB", "OW", "SQ" };
        NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat; 

        // (7fe0,0010) Pixel Data
        public int[] pixelData;

        // (0028,0010) Rows
        public int rows;

        // (0028,0011) Columns
        public int cols;

        // (0028,0101) Bits Stored
        public int bitsStored;

        // (0028,0100) Bits Allocated
        public int bitsAllocated;

        // (0028,1053) Rescale Slope
        public float rescaleSlope;

        // (0028,1052) Rescale Intercept
        public float rescaleIntercept;

        // (0028,0030) Pixel Spacing [2]
        public float[] pixelSpacing = new float[2];

        // (0018,0050) Slice Thickness
        public float sliceThickness;

        // (0018,0088) Spacing Between Slices
        public float spacingBetweenSlices;

        // (0020,0032) Image Position (Patient) [3]
        public float[] imagePosition = new float[3];

        // (0028,1050) Window Center
        public float windowCenter;

        // (0028,1051) Window Width
        public float windowWidth;

        // (0020,1041) Slice Location
        public float sliceLocation;

        public void LoadFile(byte[] bytes)
        {
            bool found = false;
            int i;

            // Szukanie DICM
            for (i = 0; i + 3 < bytes.Length; i += 4)
            {
                if (bytes[i] == 'D' && bytes[i + 1] == 'I' && bytes[i + 2] == 'C' && bytes[i + 3] == 'M')
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Debug.WriteLine("DICM not found");
                return;
            }

            int skip = 0;
            int count = 0;
            int tagGroup;
            int tagNumber;

            for (i += 4; i < bytes.Length; i += skip)
            {
                tagGroup = (bytes[i + 1] << 8) + bytes[i];
                tagNumber = (bytes[i + 3] << 8) + bytes[i + 2];

                string dataType = "";
                dataType += (char)bytes[i + 4];
                dataType += (char)bytes[i + 5];

                skip = 6; // 6 powyższych bajtów

                if (specialTags.Contains(dataType))
                {
                    count = (bytes[i + 11] << 24) + (bytes[i + 10] << 16) + (bytes[i + 9] << 8) + bytes[i + 8];
                    Debug.WriteLine(count + " 1");
                    skip += 4;
                }
                else
                {
                    Debug.WriteLine(count + " 2");
                    count = (bytes[i + 7] << 8) + bytes[i + 6];
                }
                skip += 2 + count;

                // (0028,0010) Rows
                if (tagGroup == 0x0028 && tagNumber == 0x0010)
                {
                    rows = (bytes[i + skip - count + 1] << 8) + bytes[i + skip - count];
                }

                // (0028,0011) Columns
                if (tagGroup == 0x0028 && tagNumber == 0x0011)
                {
                    cols = (bytes[i + skip - count + 1] << 8) + bytes[i + skip - count];
                }

                // (0028,0101) Bits Stored
                if (tagGroup == 0x0028 && tagNumber == 0x0101)
                {
                    bitsStored = (bytes[i + skip - count + 1] << 8) + bytes[i + skip - count];
                }

                // (0028,0100) Bits Allocated
                if (tagGroup == 0x0028 && tagNumber == 0x0100)
                {
                    bitsAllocated = (bytes[i + skip - count + 1] << 8) + bytes[i + skip - count];
                }
                // (0028,1053) Rescale Slope
                if (tagGroup == 0x0028 && tagNumber == 0x1053)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    rescaleSlope = a;
                }

                // (0028,1052) Rescale Intercept
                if (tagGroup == 0x0028 && tagNumber == 0x1052)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    rescaleIntercept = a;
                }

                // (0028,0030) Pixel Spacing [2]
                if (tagGroup == 0x0028 && tagNumber == 0x0030)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);
                    float b = float.Parse(parts[1], nfi);

                    pixelSpacing[0] = a;
                    pixelSpacing[1] = b;
                }

                //(0018,0050) Slice Thickness
                if (tagGroup == 0x0018 && tagNumber == 0x0050)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    float a = float.Parse(buffer, nfi);

                    sliceThickness = a;
                }

                // (0018,0088) Spacing Between Slices
                if (tagGroup == 0x0018 && tagNumber == 0x0088)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    float a = float.Parse(buffer, nfi);

                    spacingBetweenSlices = a;
                }

                // (0020,0032) Image Position (Patient) [3]
                if (tagGroup == 0x0020 && tagNumber == 0x0032)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');

                    float a = float.Parse(parts[0], nfi);
                    float b = float.Parse(parts[1], nfi);
                    float c = float.Parse(parts[2], nfi);

                    imagePosition[0] = a;
                    imagePosition[1] = b;
                    imagePosition[2] = c;
                }

                // (0028,1050) Window Center
                if (tagGroup == 0x0028 && tagNumber == 0x1050)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    windowCenter = a;
                }

                // (0028,1051) Window Width
                if (tagGroup == 0x0028 && tagNumber == 0x1051)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    windowWidth = a;
                }

                // (0020,1041) Slice Location
                if (tagGroup == 0x0020 && tagNumber == 0x1041)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    sliceLocation = a;
                }

                // (7fe0,0010) Pixel Data
                if (tagGroup == 0x7fe0 && tagNumber == 0x0010)
                {
                    i += skip - count;
                    break;
                }
            }

            // Read Pixels
            pixelData = new int[rows * cols];

            int row = 0, col = 0;
            for (; i < bytes.Length; i += 2, col++)
            {
                pixelData[row * cols + col] = ((bytes[i + 1]) << 8) + bytes[i];
 
                if (col == cols)
                {
                    col = 0;
                    row++;
                }
            }
        }
        public string CheckPattern(byte[] bytes, int start, int end)
        {
            char[] buffer = new char[end - start + 1];
            int i = 0;
            for (; bytes[start] != bytes[end]; start++)
            {
                buffer[i] = (char)bytes[start];
                i++;
            }
            buffer[i] = '\0';
            return new string(buffer);
        }

        public byte GetColor(int x, int y, int sliderL, int sliderW)
        {
            float color = pixelData[x * cols + y] * rescaleSlope + rescaleIntercept;
            float center = windowCenter - 0.5f + sliderL;
            float width = windowWidth - 1.0f + sliderW;
            byte min = 0;
            byte max = 255;

            // Wzory z dokumentacji
            if (color <= (center - width / 2.0f))
            {
                return min;
            }
            else if (color > (center + width / 2.0f))
            {
                return max;
            }
            else
            {
                return (byte)(((color - center) / width + 0.5f) * (max - min) + min);
            }
        }
    }
}
