//----------------------------------------------------------------------------
//  Copyright (C) 2004-2017 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.VideoSurveillance;
using FaceDetection;
using Emgu.CV.Cuda;
using AOP.Common;
using System.Drawing.Imaging;
using Baidu.Aip.API;
using System.Threading;
using BaiduAIAPI.Model;

namespace VideoSurveilance
{
    public partial class VideoSurveilance : Form
    {
       
        private static VideoCapture _cameraCapture;

        private static BackgroundSubtractor _fgDetector;
        private static Emgu.CV.Cvb.CvBlobDetector _blobDetector;
        private static Emgu.CV.Cvb.CvTracks _tracker;

        private static Queue<ImageModel> FacIdentifyQueue = new Queue<ImageModel>();
        public Image faceImage;
        Thread t1;
        public VideoSurveilance()
        {
            InitializeComponent();
            Run();
        }

        void Run()
        {
            try
            {
                _cameraCapture = new VideoCapture();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            _fgDetector = new Emgu.CV.VideoSurveillance.BackgroundSubtractorMOG2();
            _blobDetector = new CvBlobDetector();
            _tracker = new CvTracks();

            Application.Idle += ProcessFrame;
        }

        void ProcessFrame(object sender, EventArgs e)
        {
            Mat frame = _cameraCapture.QueryFrame();
            Mat smoothedFrame = new Mat();
            CvInvoke.GaussianBlur(frame, smoothedFrame, new Size(3, 3), 1); //filter out noises
                                                                            //frame._SmoothGaussian(3); 
           
            #region use the BG/FG detector to find the forground mask

            Mat forgroundMask = new Mat();
            _fgDetector.Apply(smoothedFrame, forgroundMask);
            #endregion

            CvBlobs blobs = new CvBlobs();
            _blobDetector.Detect(forgroundMask.ToImage<Gray, byte>(), blobs);
            blobs.FilterByArea(100, int.MaxValue);

            float scale = (frame.Width + frame.Width) / 2.0f;
            _tracker.Update(blobs, 0.01 * scale, 5, 5);

            long detectionTime;

            List<Rectangle> faces = new List<Rectangle>();
            List<Rectangle> eyes = new List<Rectangle>();

            IImage image = (IImage)frame;//��һ�����ص�
            faceImage = frame.Bitmap;
            DetectFace.Detect(image
             , "haarcascade_frontalface_default.xml", "haarcascade_eye.xml",
              faces, eyes,
              out detectionTime);

            #region ����ʶ�� ����ʶ����ڽϴ�����ʣ�ͼƬ�� ������壬ʶ��Ч�����Ǻã�
            //Graphics g1 = Graphics.FromImage(frame.Bitmap);
            //List<FaceIdentifyModel> tempList = new List<FaceIdentifyModel>();
            //foreach (Rectangle face in faces)
            //{
            //    Image rectImage1 = ImageHelper.CaptureImage(frame.Bitmap, face);
            //    FaceIdentifyModel MoreIdentifyInfo = FaceAPI.FaceIdentify(rectImage1, tb_Group.Text.Trim(), 1, 1);//����ʶ�� һ���˵�ʶ��Ч���ȽϺ� 
            //    MoreIdentifyInfo.rect = face;
            //    tempList.Add(MoreIdentifyInfo);
            //}
            //Color color_of_pen1 = Color.Gray;
            //color_of_pen1 = Color.Yellow;
            //Pen pen1 = new Pen(color_of_pen1, 2.0f);

            //Font font1 = new Font("΢���ź�", 16, GraphicsUnit.Pixel);
            //SolidBrush drawBrush1 = new SolidBrush(Color.Yellow);


            //tb_Identify.Text = tempList.ToJson();
            //foreach (var t in tempList)
            //{
            //    g1.DrawRectangle(pen1, t.rect);

            //    if (t.result != null)
            //    {
            //        g1.DrawString(t.result[0].user_info.Replace("��", "\r\n"), font1, drawBrush1, new Point(t.rect.X + 20, t.rect.Y - 20));
            //    }

            //}
            #endregion

            #region ����ʶ��
            //���� ����ʶ�� ����Ч���Ƚϲ�
            foreach (Rectangle face in faces)
            {
                
                #region ���û�ͼ����ʾ�Լ����ı���
                Graphics g = Graphics.FromImage(frame.Bitmap);

                ImageModel tempImage = new ImageModel();
                tempImage.Rect = face;
                tempImage.Image = frame.Bitmap;

                //�ӿڲ�ѯ�ٶȲ�
                //string faceInfo = FaceAPI.FaceDetect(ImageHelper.CaptureImage(frame.Bitmap, face));//�������

                Image rectImage = ImageHelper.CaptureImage(frame.Bitmap, face);
                FaceIdentifyModel IdentifyInfo = FaceAPI.FaceIdentify(rectImage, tb_Group.Text.Trim(), 1, 1);//����ʶ�� һ���˵�ʶ��Ч���ȽϺ�                                                                                          // tb_Result.Text = faceInfo;
                tb_Identify.Text = IdentifyInfo.ToJson().ToString();
                //���û���
                Color color_of_pen = Color.Gray;
                color_of_pen = Color.Yellow;
                Pen pen = new Pen(color_of_pen, 2.0f);
                Rectangle rect = face;

                g.DrawRectangle(pen, rect);
                Font font = new Font("΢���ź�", 16, GraphicsUnit.Pixel);
                SolidBrush drawBrush = new SolidBrush(Color.Yellow);


                if (IdentifyInfo != null)
                {
                    if (IdentifyInfo.result != null)
                    {
                        for (int i = 0; i < IdentifyInfo.result.Count; i++)
                        {
                            string faceInfo = "";
                            faceInfo = IdentifyInfo.result[i].user_info.Replace("��", "\r\n");
                            //��ʾ�û���Ϣ
                            g.DrawString(faceInfo, font, drawBrush, new Point(face.X + 20, face.Y - 20));
                        }
                    }

                }

                //CvInvoke.Rectangle(frame, face, new MCvScalar(255.0, 255.0, 255.0), 2);
                //CvInvoke.PutText(frame, faceInfo, new Point(face.X + 20, face.Y - 20), FontFace.HersheyPlain, 1.0, new MCvScalar(255.0, 255.0, 255.0));

                // ����ԭʼ��ͼ
                //System.Drawing.Image ResourceImage = frame.Bitmap;
                //ResourceImage.Save(saveDir + saveFileName);



                //�̶߳��� ��������ʶ���ͼ
                QueueHelper.WriteImage(tempImage);



                //t1 = new Thread(new ThreadStart(() =>
                //{
                //    faceInfo = FaceAPI.FaceDetect(ImageHelper.CaptureImage(frame.Bitmap, face));
                //    this.Invoke(new Action(() =>
                //    {

                //        g.DrawString(faceInfo, font, drawBrush, new Point(face.X + 20, face.Y - 20));
                //    }));

                //}));

                //t1.IsBackground = true;
                //t1.Start();


                #endregion
            }
            #endregion

            #region ��Ƶ����ԭ�е�Open CV ��֧��������

            //foreach (var pair in _tracker)
            //{
            //    CvTrack b = pair.Value;

            //    #region ��Ƶ�е���open CV ��ֱ�ӻ��ı���
            //    CvInvoke.Rectangle(frame, b.BoundingBox, new MCvScalar(255.0, 255.0, 255.0), 2);
            //    CvInvoke.PutText(frame, "man,show", new Point((int)Math.Round(b.Centroid.X), (int)Math.Round(b.Centroid.Y)), FontFace.HersheyPlain, 1.0, new MCvScalar(255.0, 255.0, 255.0));
            //    if (b.BoundingBox.Width < 100 || b.BoundingBox.Height < 50)
            //    {

            //        continue;
            //    }
            //    #endregion

            //}
            #endregion

            imageBox1.Image = frame;
            imageBox2.Image = forgroundMask;
        }

        #region �Ӵ�ͼ�н�ȡһ����ͼƬ
        /// <summary>
        /// �Ӵ�ͼ�н�ȡһ����ͼƬ
        /// </summary>
        /// <param name="fromImagePath">��ԴͼƬ��ַ</param>        
        /// <param name="offsetX">��ƫ��X����λ�ÿ�ʼ��ȡ</param>
        /// <param name="offsetY">��ƫ��Y����λ�ÿ�ʼ��ȡ</param>
        /// <param name="toImagePath">����ͼƬ��ַ</param>
        /// <param name="width">����ͼƬ�Ŀ��</param>
        /// <param name="height">����ͼƬ�ĸ߶�</param>
        /// <returns></returns>
        public void CaptureImage(string fromImagePath, int offsetX, int offsetY, string toImagePath, int width, int height)
        {
            //ԭͼƬ�ļ�
            Image fromImage = Image.FromFile(fromImagePath);
            //������ͼλͼ
            Bitmap bitmap = new Bitmap(width, height);
            //������ͼ����
            Graphics graphic = Graphics.FromImage(bitmap);
            //��ȡԭͼ��Ӧ����д����ͼ��
            graphic.DrawImage(fromImage, 0, 0, new Rectangle(offsetX, offsetY, width, height), GraphicsUnit.Pixel);
            //����ͼ��������ͼ
            Image saveImage = Image.FromHbitmap(bitmap.GetHbitmap());
            //����ͼƬ
            saveImage.Save(toImagePath, ImageFormat.Png);
            //�ͷ���Դ   
            saveImage.Dispose();
            graphic.Dispose();
            bitmap.Dispose();
        }

        public void CaptureImage(Image fromImage, Rectangle rect, string toImagePath)
        {

            //������ͼλͼ
            Bitmap bitmap = new Bitmap(rect.Width, rect.Height);
            //������ͼ����
            Graphics graphic = Graphics.FromImage(bitmap);
            //��ȡԭͼ��Ӧ����д����ͼ��
            graphic.DrawImage(fromImage, 0, 0, rect, GraphicsUnit.Pixel);
            //����ͼ��������ͼ
            Image saveImage = Image.FromHbitmap(bitmap.GetHbitmap());
            //����ͼƬ
            saveImage.Save(toImagePath, ImageFormat.Png);
            //�ͷ���Դ   
            saveImage.Dispose();
            graphic.Dispose();
            bitmap.Dispose();
        }
        #endregion

        private void btn_Screenshot_Click(object sender, EventArgs e)
        {
            if (faceImage != null)
            {
                System.Drawing.Image ResourceImage = faceImage;
                string fileDir = System.Environment.CurrentDirectory + "\\Snapshot\\";
                FileHelper.CreateDir(fileDir);
                string filePath = fileDir + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
                ResourceImage.Save(filePath);
                MessageBox.Show("����ɹ���" + filePath);
            }

        }
    }
}