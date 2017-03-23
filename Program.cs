using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program
{
    const int NUM_PIXELS = 16 * 16;
    const int NUM_BITS_ORIG = 8 * NUM_PIXELS;

    static void Main(string[] args)
    {
        //Retrieve the original image:
        int[,] originalImage = GetOriginalImage();

        //set up an array for the different prediction formulas (to solve for x^ in dx = x - x^):
        Func<int, int, int, int>[] predictionFormulas = new Func<int, int, int, int>[7];
        predictionFormulas[0] = (a, b, c) => a;                 // Formula 1: x^ = a
        predictionFormulas[1] = (a, b, c) => b;                 // Formula 2: x^ = b
        predictionFormulas[2] = (a, b, c) => c;                 // Formula 3: x^ = c
        predictionFormulas[3] = (a, b, c) => a + b - c;         // Formula 4: x^ = a + b - c
        predictionFormulas[4] = (a, b, c) => a + (b - c) / 2;   // Formula 5: x^ = a + (b-c)/2
        predictionFormulas[5] = (a, b, c) => b + (a - c) / 2;   // Formula 6: x^ = b + (a-c)/2
        predictionFormulas[6] = (a, b, c) => (a + b) / 2;       // Formula 7: x^ = (a+b)/2

        //String versions of the formulas (to write into the files in order to identify which
        //  file refers to which formula).
        string[] formulasAsStrings = new string[7]
            {
                "x^ = a",
                "x^ = b",
                "x^ = c",
                "x^ = a + b - c",
                "x^ = a + (b - c) / 2",
                "x^ = b + (a - c) / 2",
                "x^ = (a + b) / 2"
            };

        for (int i = 0; i < 7; i++)
        {
            StringBuilder sb = new StringBuilder();

            //identify the prediction formula being used:
            sb.AppendLine("Prediction formula " + (i + 1).ToString() + ": " + formulasAsStrings[i]);
            sb.AppendLine();

            //Write the original image to the string:
            sb.AppendLine("Original image:");
            sb.AppendLine(ConvertToString(originalImage));

            //Step 1 in encoding: Translate pixels into differences:
            int[,] encodedDx = EncodeAsDifferences(predictionFormulas[i], originalImage);

            //write the differences to the string:
            sb.AppendLine("Image encoded as differences:");
            sb.AppendLine(ConvertToString(encodedDx));

            //Step 2 in encoding: Translate differences into Huffman codes:
            string[,] encodedHuffmans = EncodeAsHuffman(encodedDx);

            //write the huffman codes to the string:
            sb.AppendLine("Image encoded with Huffman codes:");
            sb.AppendLine(ConvertToString(encodedHuffmans,14));

            //Step 3 in encoding: Translate 16x16 array of Huffman codes into a single binary sequence:
            string binarySequence = HuffmanArrayToBinarySequence(encodedHuffmans);

            //write the binary sequence to the string:
            sb.AppendLine("Image encoded as a binary sequence:");
            sb.AppendLine(binarySequence);

            sb.AppendLine("END OF ENCODE PROCESS");
            sb.AppendLine("------------------------------------------------------");
            sb.AppendLine("DECODING ENCODED IMAGE:");

            //Step 1 in decoding: Translate the binary sequence back into a 2D array of huffman codes:
            string[,] decodedHuffmans = BinarySequenceToHuffmanArray(binarySequence);
            sb.AppendLine("Decoded image as Huffman codes");
            sb.AppendLine(ConvertToString(decodedHuffmans,14));

            //Step 2 in decoding: Translate Huffman codes back into difference values:
            int[,] decodedDx = HuffmanArrayToDifferences(decodedHuffmans);
            sb.AppendLine("Decoded image as differences:");
            sb.AppendLine(ConvertToString(decodedDx));

            //Step 3 in decoding: Translate differences back into the original image:
            int[,] decodedImage = DecodeImage(decodedDx, predictionFormulas[i]);
            sb.AppendLine("Final decoded image:");
            sb.AppendLine(ConvertToString(decodedImage));

            //Now calculate and print the compression statistics:
            sb.AppendLine();
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine("COMPRESSION STATISTICS");
            double compressionRatio = (double)NUM_BITS_ORIG / (double)binarySequence.Length;
            sb.AppendLine("Compression ratio = " + compressionRatio);

            // Calculate the number of bits per pixel:
            double avgBitsPerPixel = (double)binarySequence.Length / (double)NUM_PIXELS;
            sb.AppendLine("Average bits per pixel in compressed image = " + avgBitsPerPixel);

            //Calculate the RMS error
            double rms = CalculateRMS(decodedImage);
            sb.AppendLine("RMS error = " + rms);

            //Write the string with the results to a file:
            string path = "LosslessCodec" + (i + 1).ToString() + ".txt";
            File.WriteAllText(path, sb.ToString());

            Console.WriteLine("Results with prediction formula " + (i + 1).ToString() + " written to " + path);
        }

        Console.WriteLine("Press enter to quit...");
        Console.ReadLine();
    }

    static string ConvertToString<T>(T[,] array,int cellWidth=5)
    { //assumes it's a 16x16 array
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                if (j < 15)
                    sb.Append( array[i, j].ToString().PadRight(cellWidth));
                else
                    sb.AppendLine(array[i, j].ToString());
            }
        }
        return sb.ToString();
    }

    // Creates the original grayscale image, as a 16x16 array of integers.
    // Each integer represents a single pixel's value.
    static int[,] GetOriginalImage()
    {
        return new int[16, 16]
            {
                { 88, 88, 88, 89, 90, 91, 92, 93, 94, 95, 93, 95, 96, 98, 97, 94 },
                { 93, 91, 91, 90, 92, 93, 94, 94, 95, 95, 92, 93, 95, 95, 95, 96 },
                { 95, 95, 95, 95, 96, 97, 94, 96, 97, 96, 98, 97, 98, 99, 95, 97 },
                { 97, 96, 98, 97, 98, 94, 95, 97, 99, 100, 99, 101, 100, 100, 98, 98 },
                { 99, 100, 97, 99, 100, 100, 98, 98, 100, 101, 100, 99, 101, 102, 99, 100 },
                { 100, 101, 100, 99, 101, 102, 99, 100, 103, 102, 103, 101, 101, 100, 102, 101 },
                { 100, 102, 103, 101, 101, 100, 102, 103, 103, 105, 104, 104, 103, 104, 104, 103 },
                { 103, 105, 103, 105, 105, 104, 104, 104, 102, 101, 100, 100, 100, 101, 102, 103 },
                { 104, 104, 105, 105, 105, 104, 104, 106, 102, 103, 101, 101, 102, 101, 102, 102 },
                { 102, 105, 105, 105, 106, 104, 106, 104, 103, 101, 100, 100, 101, 102, 102, 103 },
                { 102, 105, 105, 105, 106, 104, 106, 104, 103, 101, 100, 100, 101, 102, 102, 103 },
                { 102, 105, 105, 105, 106, 104, 105, 104, 103, 101, 102, 100, 102, 102, 102, 103 },
                { 104, 105, 106, 105, 106, 104, 106, 103, 103, 102, 100, 100, 101, 102, 102, 103 },
                { 103, 105, 107, 107, 106, 104, 106, 104, 103, 101, 100, 100, 101, 102, 102, 103 },
                { 103, 105, 106, 108, 106, 104, 106, 105, 103, 101, 101, 100, 101, 103, 102, 105 },
                { 102, 105, 105, 105, 106, 104, 106, 107, 104, 103, 102, 100, 101, 104, 102, 104 }
            };
    }

    // Calculates the root-mean-squared error for the input image (compared to the original image).
    static double CalculateRMS(int[,] toCompare)
    {
        int[,] orig = GetOriginalImage();
        double meanSquaredError = 0;

        for (int i = 0; i < 16; i++)
            for (int j = 0; j < 16; j++)
                meanSquaredError += Math.Pow(orig[i, j] - toCompare[i, j], 2);
        return Math.Sqrt(meanSquaredError) / (16.0*16.0);
    }

    // This dictionary holds the key-value pairs for difference values and the Huffman
    //      binary codes that represent those difference values.
    static readonly Dictionary<int, string> HuffmanCodes = new Dictionary<int, string>
        {
            {0,"1"},
            {1,"00"},
            {-1,"011"},
            {2,"0100"},
            {-2,"01011"},
            {3,"010100"},
            {-3,"0101011"},
            {4,"01010100"},
            {-4,"010101011"},
            {5,"0101010100"},
            {-5,"01010101011"},
            {6,"010101010100"},
            {-6,"0101010101011"}
        };

    // Returns the binary Huffman code that represents the input decimal difference.
    // Returns "error" if no match found (this shouldn't happen).
    static string HuffmanEncode(int dx)
    {
        foreach (var v in HuffmanCodes)
        {
            if (v.Key == dx)
                return v.Value;
        }
        return "error"; // error code, this shouldn't happen
    }

    // Returns the decimal difference represented by the input binary Huffman code.
    // Returns 100 if no match found (this shouldn't happen).
    static int HuffmanDecode(string huffmanDx)
    {
        foreach (var v in HuffmanCodes)
        {
            if (v.Value.Equals(huffmanDx))
                return v.Key;
        }
        return 100; // error code, this shouldn't happen
    }

    // Step 1 in the encoding process: Encoding it as an array of integers with the difference values.
    static int[,] EncodeAsDifferences(Func<int, int, int, int> predictionFunction, int[,] origImage)
    { // assuming origImage is 16x16 array, normally I would check

        int[,] result = new int[16, 16];
        for (int i = 0; i < 16; i++) //rows
        {
            for (int j = 0; j < 16; j++) //columns
            {
                if (i == 0 && j == 0) // first cell: Leave as-is
                    result[i, j] = origImage[i, j];
                else if (i == 0) // first row: x^ = a
                    result[i, j] = origImage[i, j] - origImage[i, j - 1];
                else if (j == 0) // first column: x^ = b
                    result[i, j] = origImage[i, j] - origImage[i - 1, j];
                else // neither the first row nor the first column: Use the formula.
                    result[i, j] = origImage[i, j] - predictionFunction(origImage[i, j - 1], origImage[i - 1, j], origImage[i - 1, j - 1]);
            }
        }
        return result;
    }

    //Step 2: Change the difference values into huffman codes
    static string[,] EncodeAsHuffman(int[,] differences)
    {//assuming differences is 16x16 array
        string[,] result = new string[16, 16];
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                if (i == 0 && j == 0) // first cell is converted directly into 8-bit binary:
                    result[i, j] = Convert.ToString(differences[i, j], 2).PadLeft(8, '0');
                else
                    result[i, j] = HuffmanEncode(differences[i, j]);
            }
        }
        return result;
    }

    //Step 3: Combine the huffman codes into a string.
    static string HuffmanArrayToBinarySequence(string[,] huffmans)
    { // assuming huffmans is 16x16 array
        string result = "";
        for (int i = 0; i < 16; i++)
            for (int j = 0; j < 16; j++)
                result += huffmans[i, j];

        return result;
    }

    //Step 1 in decoding: Break up the binary sequence into huffman codes
    static string[,] BinarySequenceToHuffmanArray(string binarySeq)
    {
        string[,] result = new string[16, 16];
        string currString = binarySeq;

        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                if (i == 0 && j == 0)
                {
                    result[i, j] = currString.Substring(0, 8);
                    currString = currString.Remove(0, 8);
                }
                else
                {
                    foreach (var v in HuffmanCodes)
                    {
                        if (currString.StartsWith(v.Value))
                        {
                            result[i, j] = v.Value;
                            currString = currString.Remove(0, v.Value.Length);
                            break;
                        }
                    }
                }
            }
        }
        return result;
    }

    //Step 2 in decoding: Translate the huffman codes into the difference values
    static int[,] HuffmanArrayToDifferences(string[,] huffmanArray)
    {
        int[,] result = new int[16, 16];

        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                if (i == 0 && j == 0)
                    result[i, j] = Convert.ToInt32(huffmanArray[i, j], 2);
                else
                    result[i, j] = HuffmanDecode(huffmanArray[i, j]);
            }
        }
        return result;
    }

    // Step 3 in decoding: Calculate original pixel value using difference value and original prediction function.
    static int[,] DecodeImage(int[,] decodedDifferences, Func<int, int, int, int> predictionFunction)
    {
        int[,] result = new int[16, 16];

        for (int i = 0; i < 16; i++) // rows
        {
            for (int j = 0; j < 16; j++) // columns
            {
                if (i == 0 && j == 0)
                    result[i, j] = decodedDifferences[i, j];
                else if (i == 0) // first row, x^ = a & x = x^ + dx, so x = a + dx
                    result[i, j] = result[i, j - 1] + decodedDifferences[i, j];
                else if (j == 0) // first column: x^ = b & x = x^ + dx, so x = b + dx
                    result[i, j] = result[i - 1, j] + decodedDifferences[i, j];
                else // neither first column nor first row: x^ = function & x = x^ + dx, so x = function + dx
                    result[i, j] = predictionFunction(result[i, j - 1], result[i - 1, j], result[i - 1, j - 1]) + decodedDifferences[i, j];
            }
        }
        return result;
    }
}
