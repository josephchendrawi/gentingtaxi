using GentingTaxi.Models;
using GentingTaxiApi.Interface.Types;
using GentingTaxiApi.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GentingTaxi
{
    public static class Helper
    {
        public const int SCALE_SIZE = 200;

        public static string FileUpload(HttpPostedFileBase FileModel)
        {
            string datetime = DateTime.Now.ToString("yyyyMMddhhmmss");
            string uploadedPath = "";
            if (FileModel.ContentLength > 0)
            {
                var fileName = Path.GetFileName(FileModel.FileName);
                fileName = datetime + "-" + fileName;
                fileName = fileName.Replace(" ", "-");
                //upload
                uploadedPath = fileName;
                var path = Path.Combine(HttpContext.Current.Server.MapPath("~/Content/upload"), fileName);
                FileModel.SaveAs(path);

                if (FileModel.ContentType.Contains("image"))
                {
                    //save original image first
                    FileModel.SaveAs(Path.Combine(HttpContext.Current.Server.MapPath("~/Content/upload"), "ORI_" + fileName));

                    ResizeImage(path);
                }
            }
            else
            {
                throw new Exception("Invalid File.");
            }

            return uploadedPath;
        }

        public static void ResizeImage(string Path)
        {
            Image imgOriginal = Image.FromFile(Path);
            Image imgResized;
            //scaling
            if (imgOriginal.Width > imgOriginal.Height)
            {
                imgResized = Scale(imgOriginal, 0, SCALE_SIZE);
            }
            else
            {
                imgResized = Scale(imgOriginal, SCALE_SIZE, 0);
            }
            //cropping
            imgResized = Crop(imgResized, SCALE_SIZE, SCALE_SIZE);
            imgOriginal.Dispose();
            imgResized.Save(Path);
            imgResized.Dispose();
        }
        public static Image Scale(Image imgPhoto, int Width = 0, int Height = 0)
        {
            float sourceWidth = imgPhoto.Width;
            float sourceHeight = imgPhoto.Height;
            float destHeight = 0;
            float destWidth = 0;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            // force resize, might distort image
            if (Width != 0 && Height != 0)
            {
                destWidth = Width;
                destHeight = Height;
            }
            // change size proportially depending on width or height
            else if (Height != 0)
            {
                destWidth = (float)(Height * sourceWidth) / sourceHeight;
                destHeight = Height;
            }
            else
            {
                destWidth = Width;
                destHeight = (float)(sourceHeight * Width / sourceWidth);
            }

            Bitmap bmPhoto = new Bitmap((int)destWidth, (int)destHeight,
                                        PixelFormat.Format32bppPArgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, (int)destWidth, (int)destHeight),
                new Rectangle(sourceX, sourceY, (int)sourceWidth, (int)sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();

            return bmPhoto;
        }
        public static Image Crop(Image Image, int Height, int Width)
        {
            ImageCodecInfo jpgInfo = ImageCodecInfo.GetImageEncoders()
                             .Where(codecInfo =>
                             codecInfo.MimeType == "image/jpeg").First();
            Image finalImage = Image;
            Bitmap bitmap = null;

            int left = 0;
            int top = 0;
            int srcWidth = Width;
            int srcHeight = Height;
            bitmap = new System.Drawing.Bitmap(Width, Height);
            double croppedHeightToWidth = (double)Height / Width;
            double croppedWidthToHeight = (double)Width / Height;

            if (Image.Width > Image.Height)
            {
                srcWidth = (int)(Math.Round(Image.Height * croppedWidthToHeight));
                if (srcWidth < Image.Width)
                {
                    srcHeight = Image.Height;
                    left = (Image.Width - srcWidth) / 2;
                }
                else
                {
                    srcHeight = (int)Math.Round(Image.Height * ((double)Image.Width / srcWidth));
                    srcWidth = Image.Width;
                    top = (Image.Height - srcHeight) / 2;
                }
            }
            else
            {
                srcHeight = (int)(Math.Round(Image.Width * croppedHeightToWidth));
                if (srcHeight < Image.Height)
                {
                    srcWidth = Image.Width;
                    top = (Image.Height - srcHeight) / 2;
                }
                else
                {
                    srcWidth = (int)Math.Round(Image.Width * ((double)Image.Height / srcHeight));
                    srcHeight = Image.Height;
                    left = (Image.Width - srcWidth) / 2;
                }
            }
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Image, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                new Rectangle(left, top, srcWidth, srcHeight), GraphicsUnit.Pixel);
            }
            finalImage = bitmap;

            return finalImage;
        }

        public static List<SelectListItem> GetEnumItemList(string EnumName)
        {
            List<SelectListItem> ItemList = new List<SelectListItem>();
            Type enumType = null;
            if (EnumName == "UserStatus")
                enumType = typeof(UserStatus);
            else if (EnumName == "DriverStatus")
                enumType = typeof(DriverStatus);
            else if (EnumName == "BookingStatus")
                enumType = typeof(BookingStatus);
            else if (EnumName == "Cartype")
                enumType = typeof(Cartype);
            else if (EnumName == "Gender")
                enumType = typeof(Gender);


            if (enumType != null)
            {
                foreach (var v in Enum.GetValues(enumType))
                {
                    ItemList.Add(new SelectListItem()
                    {
                        Text = v.ToString(),
                        Value = ((int)v).ToString()
                    });
                }
            }

            return ItemList;
        }

        public static string TimeAgo(DateTime dt)
        {
            TimeSpan span = DateTime.Now - dt;
            if (span.Days > 365)
            {
                int years = (span.Days / 365);
                if (span.Days % 365 != 0)
                    years += 1;
                return String.Format("{0} {1} ago",
                years, years == 1 ? "year" : "years");
            }
            if (span.Days > 30)
            {
                int months = (span.Days / 30);
                if (span.Days % 31 != 0)
                    months += 1;
                return String.Format("{0} {1} ago",
                months, months == 1 ? "month" : "months");
            }
            if (span.Days > 0)
                return String.Format("{0} {1} ago",
                span.Days, span.Days == 1 ? "day" : "days");
            if (span.Hours > 0)
                return String.Format("{0} {1} ago",
                span.Hours, span.Hours == 1 ? "hour" : "hours");
            if (span.Minutes > 0)
                return String.Format("{0} {1} ago",
                span.Minutes, span.Minutes == 1 ? "minute" : "minutes");
            if (span.Seconds > 5)
                return String.Format("{0} seconds ago", span.Seconds);
            if (span.Seconds <= 5)
                return "just now";
            return string.Empty;
        }

        public static string GetUploadURL(string Filename)
        {
            if (Filename == null || Filename.Trim() == "")
            {
                return "";
            }
            else
            {
                string UploadPath = ConfigurationManager.AppSettings["UploadPath"];
                return UploadPath + Filename;
            }
        }

        public static int GetUnreadNotificationCount(int AdminId)
        {
            AdminNotificationService AdminNotificationService = new AdminNotificationService();
            return AdminNotificationService.GetUnreadAdminNotificationCount(AdminId);
        }

        public static List<AdminNotification> GetLatestNotification(int AdminId)
        {
            AdminNotificationService AdminNotificationService = new AdminNotificationService();

            List<AdminNotification> AdminNotificationList = new List<AdminNotification>();

            var List = AdminNotificationService.GetLatestAdminNotification(AdminId, 10);
            foreach (var v in List)
            {
                AdminNotification VM = new AdminNotification();
                VM.AdminId = v.adminId;
                VM.AdminNotificationId = v.adminNotificationId;
                VM.isRead = v.isRead;
                VM.Message = v.message;
                try
                {
                    VM.BookingID = int.Parse(v.message.Replace("New Booking Transaction Created with Booking ID : ", ""));
                }
                catch
                {
                    VM.BookingID = 0;
                }

                AdminNotificationList.Add(VM);
            }

            return AdminNotificationList;
        }
    }
}