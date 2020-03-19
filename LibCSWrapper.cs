using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace CatsEyeViewer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VideoFrameHeader_T
    {
        public ushort V;   //Version
        public uint INum;  //I-Frame Number
        public ushort PNum;    //P-Frame Number
        public uint DL;    //Data Length
        public ushort D;   //Detection Result
        public ushort H;   //Channel Info
        public ushort Yr;  //Year
        public ushort Mt;  //Month
        public ushort Dy;  //Day
        public ushort Hr;  //Hour
        public ushort Mi;  //Minute
        public ushort Sc;  //Second
        public ushort Ms;  //MilliSec
        public byte VL;   //VideoLoss
        public ushort HE;  //Reserved
        public uint Rv;    //Reserved
        public IntPtr pImageData;
    }

    class LibCSWrapper
    {
        [DllImport("iJoonCore.dll")]
        public static extern int Attach(int nIndex, char[] sServerIP, int nServerPort, int nServerChannel, StreamCallback pFunc);
        [DllImport("iJoonCore.dll")]
        public static extern int Control(int nIndex, int nOpcode, IntPtr pArg, int nArgSize);
        [DllImport("iJoonCore.dll")]
        public static extern int Detach(int nIndex);
        public delegate void StreamCallback(int nIndex, int nOpcode, IntPtr pArg, int nArgSize);
    }

    public enum STREAM_CTRL_OPCODE
    {
        STREAM_CTRL_RTS_ENABLE_VIDEO = 0,
        STREAM_CTRL_RTS_ENABLE_AUDIO = 1,

        STREAM_CTRL_RTS_PAUSE = 3,
        STREAM_CTRL_RTS_RESUME = 4,

        STREAM_CTRL_RTS_START = 5,
        STREAM_CTRL_RTS_STOP = 6,

        STREAM_CTRL_RTS_GET_STATUS = 7,

        STREAM_CTRL_JPG_GET = 8,

        STREAM_CTRL_PTS_SET_TIME = 9,
        STREAM_CTRL_PTS_PAUSE = 10,
        STREAM_CTRL_PTS_RESUME = 11,

        STREAM_CTRL_PTS_START = 12,
        STREAM_CTRL_PTS_STOP = 13
    }

    public enum STREAM_CALLBACK_OPCODE
    {
        /*
            COMMON
        */
        STREAM_CALLBACK_ATTACH = 0,
        STREAM_CALLBACK_DETACH = 1,

        STREAM_CALLBACK_RTS_TRYING = 2,
        STREAM_CALLBACK_RTS_CONNECTED = 3,
        STREAM_CALLBACK_RTS_DISCONNECTED = 4,
        STREAM_CALLBACK_RTS_CONNECT_FAILED = 5,

        STREAM_CALLBACK_RTS_SET_VINIT = 6,
        STREAM_CALLBACK_RTS_SET_VFRAME = 7,

        STREAM_CALLBACK_RTS_SET_AINIT = 8,
        STREAM_CALLBACK_RTS_SET_AFRAME = 9,

        STREAM_CALLBACK_JPG_SET = 10,
        STREAM_CALLBACK_BLE_SET = 11,

        STREAM_CALLBACK_PTS_TRYING = 12,
        STREAM_CALLBACK_PTS_CONNECTED = 13,
        STREAM_CALLBACK_PTS_DISCONNECTED = 14,
        STREAM_CALLBACK_PTS_CONNECT_FAILED = 15,


        STREAM_CALLBACK_PTS_SET_VINIT = 16,
        STREAM_CALLBACK_PTS_SET_VFRAME = 17,

        STREAM_CALLBACK_PTS_SET_AINIT = 18,
        STREAM_CALLBACK_PTS_SET_AFRAME = 19,

        STREAM_CALLBACK_PTS_SET_TIMELIST = 20,
        STREAM_CALLBACK_PTS_SET_EOF = 21
    };
}
