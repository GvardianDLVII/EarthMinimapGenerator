using Elskom.Generic.Libs;
using Ieo.EarthFileApi.Files;
using Ieo.EarthMinimapGenerator;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EarthMinimapGenerator.Tests
{
   internal class EarthDecompressor
   {
      internal static IEnumerable<byte[]> ReadSections(byte[] compressedData)
      {
         int bytesProcessed = 0;
         int section = 0;
         while (bytesProcessed < compressedData.Length)
         {
            Decompress(compressedData.Skip(bytesProcessed).ToArray(), out var outData, out var bytesRead);
            bytesProcessed += bytesRead;
            File.WriteAllBytes($"Section_{section}.dat", outData);
            section++;
            yield return outData;
         }
      }

      private static void Decompress(byte[] inData, out byte[] outData, out int bytesRead)
      {
         using MemoryStream memoryStream = new MemoryStream();
         using ZOutputStream zOutputStream = new ZOutputStream(memoryStream);
         using Stream stream = new MemoryStream(inData);
         try
         {
            stream.CopyTo(zOutputStream);
         }
         catch (ZStreamException)
         {
         }

         try
         {
            zOutputStream.Flush();
         }
         catch (StackOverflowException ex2)
         {
            throw new NotPackableException("Decompression Failed due to a stack overflow.", ex2);
         }

         try
         {
            zOutputStream.Finish();
         }
         catch (ZStreamException ex3)
         {
            throw new NotUnpackableException("Decompression Failed.", ex3);
         }

         bytesRead = (int)zOutputStream.TotalIn;
         outData = memoryStream.ToArray();
      }
   }

   public class Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void Test1()
      {
         const string MapName = "( 6) Erotomania 1-3 [Ani]";

         var lndFile = EarthFileReader.ReadLndFile(@$"..\..\..\Samples\{MapName}.lnd");
         var misFile = EarthFileReader.ReadMisFile(@$"..\..\..\Samples\{MapName}.mis");
         MinimapGenerator.Generate(lndFile, misFile, new MinimapOptions
         {
            Scale = 1,
            MarkStartingPositions = false
         }).Save(@$"..\..\..\Samples\{MapName}.png");
      }

      [Test]
      public void test2()
      {
         //byte[][] array3 = EarthDecompressor.ReadSections(File.ReadAllBytes("D:\\SteamLibrary\\steamapps\\common\\Earth 2150 The Moon Project\\Levels\\( 0) Test Map.mis")).ToArray();

         //byte[][] array = EarthDecompressor.ReadSections(File.ReadAllBytes("D:\\SteamLibrary\\steamapps\\common\\Earth 2150 The Moon Project\\Archive\\Temp_20230414161718\\00000000.tmp")).ToArray();
         byte[][] array2 = EarthDecompressor.ReadSections(File.ReadAllBytes("D:\\SteamLibrary\\steamapps\\common\\Earth 2150 The Moon Project\\Temp\\00000000.tmp")).ToArray();
      }
   }
}