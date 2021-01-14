﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Point = OpenCvSharp.Point;


namespace ChessHelper
{
    class OpenCV
    {
        private readonly Writer console;
        private readonly Autoit autoIt;
        private readonly Overlay overlay;
        public Dictionary<string, Bitmap> figures;
        Bitmap gameScreen_bitmap;
        Graphics gameScreen_graphics;
        System.Drawing.Size size_region;



        public OpenCV(Writer console, Autoit autoIt, Overlay overlay)
        {
            this.console = console;
            this.autoIt = autoIt;
            this.overlay = overlay;

            gameScreen_bitmap = new Bitmap(528, 528, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            gameScreen_graphics = Graphics.FromImage(gameScreen_bitmap);
            size_region = new System.Drawing.Size(528, 528);

            LoadImages(@"\figures", out figures);

            console.WriteLine("OpenCV loaded.");
        }

        internal Figure FigureDefinition(Bitmap field)
        {
            double threshold = 0.90;
            using Mat fieldImg = OpenCvSharp.Extensions.BitmapConverter.ToMat(field);       //Сохраняем скрин экрана в mat
            using Mat fieldImgGray = fieldImg.CvtColor(ColorConversionCodes.BGR2GRAY);
            

            foreach (KeyValuePair<string, Bitmap> figure in figures)
            {
                using Mat searchFigureImg = OpenCvSharp.Extensions.BitmapConverter.ToMat(figure.Value);
                using Mat searchFigureImgGray = searchFigureImg.CvtColor(ColorConversionCodes.BGR2GRAY);
                using Mat result = new Mat();
               
                Cv2.MatchTemplate(fieldImgGray, searchFigureImgGray, result, TemplateMatchModes.CCoeffNormed);        //Поиск шаблона
                Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Tozero);
                Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc); //Поиск точки

                if (maxVal > threshold)
                {
                  //  console.WriteLine(maxVal);

                    switch (figure.Key)
                    {
                        case "b_rook_1":
                        case "b_rook_2":
                            return Figure.rook;
                        case "b_pawn_1":
                        case "b_pawn_2":
                            return Figure.pawn;
                        case "b_knight_1":
                        case "b_knight_2":
                            return Figure.knight;
                        case "b_elephant_1":
                        case "b_elephant_2":
                            return Figure.elephant;
                        case "b_queen_1":
                        case "b_queen_2":
                            return Figure.queen;
                        case "b_king_1":
                        case "b_king_2":
                            return Figure.king;

                        case "w_rook_1":
                        case "w_rook_2":
                            return Figure.Rook;
                        case "w_pawn_1":
                        case "w_pawn_2":
                            return Figure.Pawn;
                        case "w_knight_1":
                        case "w_knight_2":
                            return Figure.Knight;
                        case "w_elephant_1":
                        case "w_elephant_2":
                            return Figure.Elephant;
                        case "w_queen_1":
                        case "w_queen_2":
                            return Figure.Queen;
                        case "w_king_1":
                        case "w_king_2":
                            return Figure.King;

                        default:
                            return Figure.empty;
                    }

                }


            }

            return Figure.empty;
        }

        internal void GetFieldsImage(int x, int y, SquareField[][] fc)
        {
            int counter = 0;
            for (int j = 0; j < 8; j++)
            {
                for (int i = 0; i < 8; i++)
                {
                    System.Drawing.Size fieldSize = new System.Drawing.Size(66, 66);
                    Bitmap fieldImage = new Bitmap(66, 66, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    using Graphics fieldGraph = Graphics.FromImage(fieldImage);
                    fieldGraph.CopyFromScreen(x + i * 66, y + j * 66, 0, 0, fieldSize);
                    fc[j][i].image = fieldImage;
                   
                }
            }

        }




        private bool LoadImages(string folderName, out Dictionary<string, Bitmap> images)
        {
            string imgFolderDirPath = Directory.GetCurrentDirectory() + folderName;
            images = new Dictionary<string, Bitmap>();

            if (!Directory.Exists(imgFolderDirPath))
            {
                console.WriteLine("Error. Image directory not founded.");
                return false;
            }

            string[] imgPaths = Directory.GetFiles(imgFolderDirPath, "*.png");

            for (int i = 0; i < imgPaths.Length; i++)
            {
                if (!File.Exists(imgPaths[i]))
                {
                    MessageBox.Show($"Error load image: {imgPaths[i]}", "Error.");
                    return false;
                }

                string name = imgPaths[i];
                name = name.Replace(imgFolderDirPath, "").Replace("\\", "").Replace(".png", "");
                images.Add(name, new Bitmap(imgPaths[i]));
            }

            return true;
        }


        internal bool SearchImageFromDict(Dictionary<string, Bitmap> buttonImages, out Point centerPoint, out string name)
        {
            double threshold = 0.85;
            name = null;
            centerPoint = new OpenCvSharp.Point();

            gameScreen_graphics.CopyFromScreen(528, 528, 0, 0, size_region);
            using Mat gameScreen = OpenCvSharp.Extensions.BitmapConverter.ToMat(gameScreen_bitmap);       //Сохраняем скрин экрана в mat
            using Mat mat_region_desktop_gray = gameScreen.CvtColor(ColorConversionCodes.BGR2GRAY);


            foreach (KeyValuePair<string, Bitmap> buttonImage in buttonImages)
            {
                using Mat searchImg = OpenCvSharp.Extensions.BitmapConverter.ToMat(buttonImage.Value);
                using Mat searchImg_gray = searchImg.CvtColor(ColorConversionCodes.BGR2GRAY);
               
                using Mat result = new Mat();
                Cv2.MatchTemplate(mat_region_desktop_gray, searchImg_gray, result, TemplateMatchModes.CCoeffNormed);        //Поиск шаблона
                Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Tozero);
                Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc); //Поиск точки
                if (maxVal > threshold)
                {
                    centerPoint = new OpenCvSharp.Point(maxLoc.X + buttonImage.Value.Width / 2, maxLoc.Y + buttonImage.Value.Height / 2);
                    overlay.DrawRect(maxLoc.X, maxLoc.Y, buttonImage.Value.Width, buttonImage.Value.Height);
                    name = buttonImage.Key;
                    return true;
                }
            }

            return false;
        }

        //internal bool SearchImageFromRegion(Bitmap bitmap, out Point f, Point start, Point end)
        //{
        //    int widthRegion = end.X - start.X;
        //    int heightRegion = end.Y - start.Y;

        //    double threshold = 0.9;        //Пороговое значение SearchImg
        //    f = new OpenCvSharp.Point();

        //    if (bitmap == null)
        //    {
        //        console.WriteLine("Error. Null bitmap.");
        //        return false;
        //    }

        //    if (widthRegion < 1 || heightRegion < 1 || end.X > window.Width || end.Y > window.Height)
        //    {
        //        console.WriteLine("Error region.");
        //        return false;
        //    }


        //    using Bitmap gameRegion_bitmap = new Bitmap(widthRegion, heightRegion, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        //    using Graphics gameRegion_graphics = Graphics.FromImage(gameRegion_bitmap);

        //    System.Drawing.Size region = new System.Drawing.Size(end.X - start.X, end.Y - start.Y);
        //    gameRegion_graphics.CopyFromScreen(window.X + start.X, window.Y + start.Y, 0, 0, region);                    //делаем скрин экрана


        //    using Mat gameScreen = OpenCvSharp.Extensions.BitmapConverter.ToMat(gameRegion_bitmap);  //Сохраняем скрин экрана в mat
        //    using Mat gameScreenGrayRegion = gameScreen.CvtColor(ColorConversionCodes.BGR2GRAY);

        //    //Cv2.ImShow("Matches", gameScreen);
        //    //Cv2.WaitKey();

        //    using Mat searchImg = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
        //    using Mat searchImgGray = searchImg.CvtColor(ColorConversionCodes.BGR2GRAY);

        //    using Mat result = new Mat();

        //    Cv2.MatchTemplate(gameScreenGrayRegion, searchImgGray, result, TemplateMatchModes.CCoeffNormed);        //Поиск шаблона
        //    Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Tozero);
        //    Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc); //Поиск точки

        //    if (maxVal > threshold)
        //    {
        //        f = new OpenCvSharp.Point(maxLoc.X + start.X, maxLoc.Y + start.Y);
        //        // overlay.DrawRect(maxLoc.X + start.X, maxLoc.Y+ start.Y, bitmap.Width, bitmap.Height);
        //    }
        //    else
        //        return false;

        //    return true;
        //}

        //public bool SearchBitmapPos(Bitmap bitmap, out Point sourccePoint, out Point centerPoint)
        //{
        //    window = autoIt.window;
        //    double threshold = 0.85;        //Пороговое значение SearchImg
        //    sourccePoint = new Point();
        //    centerPoint = new Point();
        //    if (bitmap == null)
        //        return false;

        //    gameScreen_graphics.CopyFromScreen(window.X, window.Y, 0, 0, size_region);                         //делаем скрин экрана

        //    using Mat resultImg = new Mat();
        //    using Mat gameScreen = OpenCvSharp.Extensions.BitmapConverter.ToMat(gameScreen_bitmap);       //Сохраняем скрин экрана в mat
        //    using Mat searchImg = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
        //    using Mat gameScreenGray = gameScreen.CvtColor(ColorConversionCodes.BGR2GRAY);
        //    using Mat searchImgGray = searchImg.CvtColor(ColorConversionCodes.BGR2GRAY);

        //    Cv2.MatchTemplate(gameScreenGray, searchImgGray, resultImg, TemplateMatchModes.CCoeffNormed);        //Поиск шаблона
        //    Cv2.Threshold(resultImg, resultImg, threshold, 1.0, ThresholdTypes.Tozero);                                    //Оптимизация
        //    Cv2.MinMaxLoc(resultImg, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc); //Поиск точки

        //    if (maxVal > threshold)
        //    {
        //        sourccePoint = maxLoc;
        //        centerPoint = new Point(maxLoc.X + bitmap.Width / 2, maxLoc.Y + bitmap.Height / 2);
        //        overlay.DrawRect(maxLoc.X, maxLoc.Y, bitmap.Width, bitmap.Height);
        //    }
        //    else
        //        return false;

        //    return true;
        //}

        //internal bool SearchImagesFromRegion(Bitmap bitmap, out List<Point> points, Point start, Point end)
        //{
        //    window = autoIt.window;
        //    double threshold = 0.85;  //Пороговое значение SearchImg
        //    points = new List<Point>();

        //    if (bitmap == null)
        //    {
        //        console.WriteLine("Error. Null bitmap.");
        //        return false;
        //    }

        //    if (start.X >= end.X || start.Y >= end.Y || end.X > window.Width || end.Y > window.Height)
        //    {
        //        console.WriteLine("Error region.");
        //        return false;
        //    }

        //    gameScreen_graphics.CopyFromScreen(window.X, window.Y, 0, 0, size_region);               //делаем скрин экрана
        //    using Mat gameScreen = OpenCvSharp.Extensions.BitmapConverter.ToMat(gameScreen_bitmap);  //Сохраняем скрин экрана в mat
        //    using Mat gameScreenGray = gameScreen.SubMat(start.Y, end.Y, start.X, end.X);            //Вырезаем область
        //    using Mat gameScreenGrayRegion = gameScreenGray.CvtColor(ColorConversionCodes.BGR2GRAY); //Конвертируем в ЧБ  

        //    using Mat searchImg = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
        //    using Mat searchImgGray = searchImg.CvtColor(ColorConversionCodes.BGR2GRAY);

        //    using Mat result = new Mat();

        //    Cv2.MatchTemplate(gameScreenGrayRegion, searchImgGray, result, TemplateMatchModes.CCoeffNormed);
        //    Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Tozero);

        //    using Mat resultPoints = new Mat();
        //    Cv2.FindNonZero(result, resultPoints);

        //    for (int i = 0; i < resultPoints.Total(); i++)
        //    {
        //        points.Add(new Point(resultPoints.At<Point>(i).X + start.X, resultPoints.At<Point>(i).Y + start.Y));
        //    }

        //    //Cv2.ImShow("Matches", result);
        //    //Cv2.WaitKey();

        //    return true;
        //}

    }
}