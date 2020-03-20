using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;

namespace CatsEyeViewer
{
    public struct FrameData
    {
        public FrameData(int index, IntPtr framePtr, int length)
        {
            mIndex = index;
            mFramePtr = framePtr;
            mLength = length;
        }

        public int mIndex;
        public IntPtr mFramePtr;
        public int mLength;
    }

    public partial class MainForm : Form
    {
        static List<FrameData> mList;
        byte[] byteImg;
        byte[] byteImg2;
        static bool cameraIsStart;
        static bool isFormClosing = false;
        Thread mThread;

        public MainForm()
        {
            InitializeComponent();
            
            TestCircular();

            cameraIsStart = false;
            mList = new List<FrameData>();
            byteImg = new byte[1280 * 720 * 24 + 54];
            byteImg2 = new byte[1280 * 720 * 24 + 54];
        }

        private void Connect_Button_Click(object sender, EventArgs e)
        {
            client.Attach(textBox1.Text, Convert.ToInt32(textBox2.Text));

            if (!cameraIsStart)
            {
                cameraIsStart = true;
                mThread = new Thread(RenderThread);
                mThread.Start();
                
                Connect_Button.Text = "Disconnect";
            }
            else
            {
                lock (mList)
                {
                    cameraIsStart = false;
                }
                mThread.Join(100);

                Connect_Button.Text = "Connect";

            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            isFormClosing = true;
            cameraIsStart = false;
            mThread.Join(1000);
        }

        public Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }

        public void RenderThread()
        {
            lock (mList)
            {
                mList.Clear();
            }

            while (!isFormClosing)
            {
                if (!cameraIsStart)
                {
                    lock (mList)
                    {
                        mList.Clear();
                    }
                    break;
                }

                lock(mList)
                {
                    if (mList.Count > 0)
                    {
                        FrameData frameData = mList.First();
                        mList.RemoveAt(0);
                        if (frameData.mFramePtr != IntPtr.Zero)
                        {
                            Console.WriteLine("frameData.mIndex : " + frameData.mIndex + ", listCount: " + mList.Count);
                            switch (frameData.mIndex)
                            {
                                case 1:
                                    Marshal.Copy(frameData.mFramePtr, byteImg, 0, frameData.mLength);
                                    pictureBox1.InvokeIfNeeded(setImage1, ByteArrayToImage(byteImg));
                                    break;
                                case 2:
                                    Marshal.Copy(frameData.mFramePtr, byteImg2, 0, frameData.mLength);
                                    pictureBox2.InvokeIfNeeded(setImage2, ByteArrayToImage(byteImg2));
                                    break;
                            }

                            /*
                            try
                            {
                                Marshal.Copy(frameData.mFramePtr, byteImg, 0, frameData.mLength);
                            }
                            catch (System.AccessViolationException e)
                            {
                                Console.WriteLine("AccessViolationException Occured");
                            }

                            if (pictureBox1.IsDisposed || pictureBox1.Disposing)
                            {

                            }
                            else
                            {
                                switch(frameData.mIndex)
                                {
                                    case 1:
                                        pictureBox1.InvokeIfNeeded(setImage1, ByteArrayToImage(byteImg));
                                        break;
                                    case 2:
                                        pictureBox2.InvokeIfNeeded(setImage2, ByteArrayToImage(byteImg));
                                        break;
                                    
                                }
                                
                            }
                            */

                        }
                    }
                    else {
                        Thread.Sleep(20);
                    }
                }    
            }
            Console.WriteLine("RenderThread Exit");
        }

        public void setImage1(Image img)
        {
            if (pictureBox1.Image != null)
                pictureBox1.Image.Dispose();
            pictureBox1.Image = img;//.Clone() as Image;
        }

        public void setImage2(Image img)
        {
            if (pictureBox2.Image != null)
                pictureBox2.Image.Dispose();
            pictureBox2.Image = img;//.Clone() as Image;
        }

        public Image ByteArrayToImage(byte[] byteArrayIn)
        {
            using (var ms = new MemoryStream(byteArrayIn))
            {
                return Image.FromStream(ms);
            }
        }
    }

    
    public static class ControlExtensions
    {
        public static void InvokeIfNeeded(this Control control, Action action)
        {
            if(control.InvokeRequired)
                control.BeginInvoke(action);
            else
                action();
        }

        public static void InvokeIfNeeded<T>(this Control control, Action<T> action, T arg)
        {
            if (control.InvokeRequired)
            {
                control.BeginInvoke(action, arg);
            }
            else
            {
                action(arg);
            }
        }
    }
}
