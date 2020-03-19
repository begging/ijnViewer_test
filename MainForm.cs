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

        LibCSWrapper.StreamCallback callback = new LibCSWrapper.StreamCallback(MyStreamCallback);

        public MainForm()
        {
            InitializeComponent();

            cameraIsStart = false;
            mList = new List<FrameData>();
            byteImg = new byte[1280 * 720 * 24 + 54];
            byteImg2 = new byte[1280 * 720 * 24 + 54];
        }

        static void MyStreamCallback(int nIndex, int nOpcode, IntPtr pArg, int nArgSize)
        {
            STREAM_CALLBACK_OPCODE opcode = (STREAM_CALLBACK_OPCODE)nOpcode;
            switch(opcode)
            {
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_ATTACH:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_ATTACH, " + nIndex);
                    LibCSWrapper.Control(nIndex, (int)STREAM_CTRL_OPCODE.STREAM_CTRL_RTS_ENABLE_VIDEO, IntPtr.Zero, 0);
                    LibCSWrapper.Control(nIndex, (int)STREAM_CTRL_OPCODE.STREAM_CTRL_RTS_START, IntPtr.Zero, 0);
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_DETACH:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_DETACH");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_RTS_TRYING:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_RTS_TRYING");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_RTS_CONNECTED:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_RTS_CONNECTED");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_RTS_DISCONNECTED:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_RTS_DISCONNECTED");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_RTS_CONNECT_FAILED:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_RTS_CONNECT_FAILED");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_RTS_SET_VINIT:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_RTS_SET_VINIT, nArgSize: " + nArgSize);
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_RTS_SET_VFRAME:
                    //Console.WriteLine("C# Callback Function : STREAM_CALLBACK_RTS_SET_VFRAME, nArgSize" + nArgSize + ", nIndex: " + nIndex);
                    
                    if (pArg != IntPtr.Zero)
                    {
                        VideoFrameHeader_T h = (VideoFrameHeader_T)Marshal.PtrToStructure(pArg, typeof(VideoFrameHeader_T));
                        
                        if (h.pImageData != IntPtr.Zero)
                        {
                            lock (mList)
                            {
                                mList.Add(new FrameData(nIndex, h.pImageData, (int)h.DL));
                            }
                        //Console.WriteLine("Year : " + h.Yr + "h.DL : " + h.DL);
                        }
                    }
                   
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_RTS_SET_AINIT:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_RTS_SET_AINIT");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_RTS_SET_AFRAME:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_RTS_SET_AFRAME");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_JPG_SET:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_JPG_SET");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_BLE_SET:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_BLE_SET");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_TRYING:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_TRYING");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_CONNECTED:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_CONNECTED");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_DISCONNECTED:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_DISCONNECTED");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_CONNECT_FAILED:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_CONNECT_FAILED");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_SET_VINIT:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_SET_VINIT");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_SET_VFRAME:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_SET_VFRAME");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_SET_AINIT:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_SET_AINIT");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_SET_AFRAME:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_SET_AFRAME");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_SET_TIMELIST:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_SET_TIMELIST");
                    break;
                case STREAM_CALLBACK_OPCODE.STREAM_CALLBACK_PTS_SET_EOF:
                    Console.WriteLine("C# Callback Function : STREAM_CALLBACK_PTS_SET_EOF");
                    break;
            }
        }

        private void Connect_Button_Click(object sender, EventArgs e)
        {
            if (!cameraIsStart)
            {
                cameraIsStart = true;
                mThread = new Thread(RenderThread);
                mThread.Start();

                LibCSWrapper.Attach(1, textBox1.Text.ToCharArray(), Convert.ToInt32(textBox2.Text), Convert.ToInt32(comboBox1.Text), callback);
                LibCSWrapper.Attach(2, textBox1.Text.ToCharArray(), 8200, Convert.ToInt32(comboBox1.Text), callback);
                
                

                Connect_Button.Text = "Disconnect";
            }
            else
            {
                lock (mList)
                {
                    cameraIsStart = false;
                }
                mThread.Join(100);


                LibCSWrapper.Control(1, (int)STREAM_CTRL_OPCODE.STREAM_CTRL_RTS_STOP, IntPtr.Zero, 0);
                LibCSWrapper.Detach(1);

                LibCSWrapper.Control(2, (int)STREAM_CTRL_OPCODE.STREAM_CTRL_RTS_STOP, IntPtr.Zero, 0);
                LibCSWrapper.Detach(2);

                Connect_Button.Text = "Connect";

            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            isFormClosing = true;
            cameraIsStart = false;
            mThread.Join(1000);

            LibCSWrapper.Control(1, (int)STREAM_CTRL_OPCODE.STREAM_CTRL_RTS_STOP, IntPtr.Zero, 0);
            LibCSWrapper.Detach(1);

            LibCSWrapper.Control(2, (int)STREAM_CTRL_OPCODE.STREAM_CTRL_RTS_STOP, IntPtr.Zero, 0);
            LibCSWrapper.Detach(2);

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
                            //Console.WriteLine("frameData.mIndex : " + frameData.mIndex + ", listCount: " + mList.Count);
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
