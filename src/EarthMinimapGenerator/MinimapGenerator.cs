using Ieo.EarthFileApi.Files;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Ieo.EarthMinimapGenerator
{
   public static class MinimapGenerator
   {
      public static Bitmap Generate(EarthFile<EarthLndData> lndFile, EarthFile<EarthMisData> misFile, MinimapOptions options)
      {
         if (options == null)
            options = MinimapOptionsFactory.Default;
         var bitmap = new Bitmap(lndFile.Data.MapWidth, lndFile.Data.MapHeight);
         var minHeight = lndFile.Data.TerrainHeight.Min();
         var maxHeight = lndFile.Data.TerrainHeight.Max();
         for (int y = 0; y < lndFile.Data.MapHeight; y++)
         {
            for (int x = 0; x < lndFile.Data.MapWidth; x++)
            {
               var waterHeight = lndFile.Data.WaterHeight[y * lndFile.Data.MapWidth + x] & 0x1fff;
               var terrainHeight = lndFile.Data.TerrainHeight[y * lndFile.Data.MapWidth + x];
               var resources = lndFile.Data.Resources[y * lndFile.Data.MapWidth + x];

               var waterColorIndex = waterHeight > terrainHeight ? Math.Min((waterHeight - terrainHeight) / 16, 0x7f) : 0;

               bool isPassable = true;
               const int passableThreshold = 0xb3;

               if (x > 0)
                  isPassable &= Math.Abs(lndFile.Data.TerrainHeight[y * lndFile.Data.MapWidth + x - 1] - terrainHeight) < passableThreshold;
               if (x < lndFile.Data.MapWidth - 1)
                  isPassable &= Math.Abs(lndFile.Data.TerrainHeight[y * lndFile.Data.MapWidth + x + 1] - terrainHeight) < passableThreshold;
               if (y > 0)
                  isPassable &= Math.Abs(lndFile.Data.TerrainHeight[(y - 1) * lndFile.Data.MapWidth + x] - terrainHeight) < passableThreshold;
               if (y < lndFile.Data.MapHeight - 1)
                  isPassable &= Math.Abs(lndFile.Data.TerrainHeight[(y + 1) * lndFile.Data.MapWidth + x] - terrainHeight) < passableThreshold;

               var color = resources > 0x70
                  ? Colors.ResourceColor
                  : waterHeight > terrainHeight
                     ? misFile.Data.WaterType == WaterType.Lava
                        ? Colors.LavaColor
                        : Colors.WaterColors[waterColorIndex]
                     : TerrainHeightToColor(terrainHeight - lndFile.Data.LevelWaterHeight, isPassable);

               bitmap.SetPixel(x, y, color);
            }
         }

         bitmap = new Bitmap(bitmap, new Size(bitmap.Width * options.Scale, bitmap.Height * options.Scale));
         if(options.MarkStartingPositions)
         {
            MarkPlayers(bitmap, misFile.Data.Players, options.Scale);
         }
         return bitmap;
      }

      private static void MarkPlayers(Bitmap bmp, IReadOnlyList<PlayerData> players, int scale)
      {
         Graphics g = Graphics.FromImage(bmp);
         g.SmoothingMode = SmoothingMode.AntiAlias;
         g.InterpolationMode = InterpolationMode.HighQualityBicubic;
         g.PixelOffsetMode = PixelOffsetMode.HighQuality;

         StringFormat stringFormat = new StringFormat();
         stringFormat.Alignment = StringAlignment.Center;
         stringFormat.LineAlignment = StringAlignment.Center;
         for (int i = 0; i < players.Count; i++)
         {
            if (players[i].UnknownField == 0) continue;
            var rect = new Rectangle(players[i].StartPositionX * scale - 10, players[i].StartPositionY * scale - 10, 20, 20);
            var pen = new Pen(Color.Black, 2);
            g.DrawEllipse(pen, rect);
            g.FillEllipse(Brushes.White, rect);
            g.DrawString((i + 1).ToString(), new Font("Consolas", 8, FontStyle.Bold), Brushes.Black, rect, stringFormat);
         }
         g.Flush();
      }
      private static Color TerrainHeightToColor(int terrainHeight, bool isPassable = false)
         => TerrainHeightToColorPassable(isPassable);
         //=> TerrainHeightToColorOriginal(terrainHeight);

      private static Color TerrainHeightToColorOriginal(int terrainHeight)
      {
         if (terrainHeight >= 0)
            return Colors.SpringTerPosColors[Math.Min(terrainHeight / 16, 0x7f)];
         return Colors.SpringTerNegColors[Math.Min(-terrainHeight / 16, 0x7f)];
      }

      private static Color TerrainHeightToColorGrayscale(int terrainHeight)
      {
         Color baseColor = terrainHeight >= 0
            ? Colors.SpringTerPosColors[Math.Min(terrainHeight / 16, 0x7f)]
            : Colors.SpringTerNegColors[Math.Min(-terrainHeight / 16, 0x7f)];

         var colorValue = ((int)baseColor.R + (int)baseColor.G + (int)baseColor.B) / 3;
         return Color.FromArgb(colorValue, colorValue, colorValue);
      }

      private static Color TerrainHeightToColorGrayscaleInverted(int terrainHeight)
      {
         Color baseColor = terrainHeight >= 0
            ? Colors.SpringTerPosColors[Math.Min(terrainHeight / 16, 0x7f)]
            : Colors.SpringTerNegColors[Math.Min(-terrainHeight / 16, 0x7f)];

         var colorValue = 256 - ((int)baseColor.R + (int)baseColor.G + (int)baseColor.B) / 3;
         return Color.FromArgb(colorValue, colorValue, colorValue);
      }

      private static Color TerrainHeightToColorPassable(bool isPassable)
      {
         return isPassable 
            ? Color.Gray
            : Color.Black;
      }
   }
}
