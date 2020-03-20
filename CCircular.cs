using System;
using System.Management;
using System.Runtime.InteropServices;
    
using Google.Protobuf;
using SCNet;
using Circular2;

namespace CatsEyeViewer {
    partial class MainForm {

        [DllImport("ffmpegWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int FFMpegCodecDecInit(int index, int picWidth, int picHeight);
        [DllImport("ffmpegWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FFMpegCodecDecUninit(int index);
        [DllImport("ffmpegWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FFMpegCodecDecExecute(int index, byte[] inbuf, int size);
        
        Client client;
        Int32 recentWidth = 0, recentHeight = 0;

        void TestCircular() {
            Console.WriteLine("test circular()");

            client = new Client();
            SetConnectionCallback();
            SetRegistry();
        }

        void SetConnectionCallback() {
            client.connectingCallback = (Client client) => {
                Console.WriteLine("connecting");
            };
            client.connectedCallback = (Client client) => {
                Console.WriteLine("connected");
                RegistrationViewerRequest req = new RegistrationViewerRequest() {
                    Serial = getUUID(),
                    SwVersion = "1.0.0",
                    PriIP = "-",
                    PriPort = 11111
                };
                client.Send(req);
            };
            client.connectingFailedCallback = (Client client) => {
                Console.WriteLine("connectingFailed");
            };
            client.disconnectedCallback = (Client client) => {
                Console.WriteLine("disconnected");
            };
        }

        void SetRegistry() {
            Registry registry = Registry.Instance;
            registry.Add(new Ping(), Ping.Parser, (int)PacketType.Ping,
                (IMessage message) => {
                    Ping ping = (Ping)message;
                    client.Send(ping);
                });
            registry.Add(new RegistrationViewerRequest(), RegistrationViewerRequest.Parser, (int)PacketType.RegistrationViewerRequest, null);
            registry.Add(new RegistrationViewerResponse(), RegistrationViewerResponse.Parser, (int)PacketType.RegistrationViewerResponse,
                (IMessage message) => {
                    RegistrationViewerResponse res = (RegistrationViewerResponse)message;
                    if (res.Result == Result.Success) {
                        Console.WriteLine("Registration Viewer success");

                        StreamingStartRequest req = new StreamingStartRequest() {
                            AccessToken = "access_token",
                            Serial = "office",
                            Uts = 0
                        };
                        client.Send(req);
                    }
                    else {
                        Console.WriteLine("Registration Viewer failed, " + res.Result);
                    }
                });
            registry.Add(new StreamingStartRequest(), StreamingStartRequest.Parser, (int)PacketType.StreamingStartRequest, null);
            registry.Add(new StreamingStartResponse(), StreamingStartResponse.Parser, (int)PacketType.StreamingStartResponse,
                (IMessage message) => {
                    StreamingStartResponse res = (StreamingStartResponse)message;
                    if (res.Result == Result.Success) {
                        Console.WriteLine("streaming start success");
                    }
                    else {
                        Console.WriteLine("streaming start failed, " + res.Result);
                    }
                });
            registry.Add(new StreamingStopRequest(), StreamingStopRequest.Parser, (int)PacketType.StreamingStopRequest, null);
            registry.Add(new StreamingStopResponse(), StreamingStopResponse.Parser, (int)PacketType.StreamingStopResponse,
               (IMessage message) => {
                   StreamingStopResponse res = (StreamingStopResponse)message;
                   Console.WriteLine("StreamingStopResponse() called. " + res.Result);
               });
            registry.Add(new VideoInitFrame(), VideoInitFrame.Parser, (int)PacketType.VideoInitFrame,
                (IMessage message) => {
                    VideoInitFrame videoInitFrame = (VideoInitFrame)message;

                    Int32 width = (Int32)videoInitFrame.Width;
                    Int32 height = (Int32)videoInitFrame.Height;
                    Int32 fps = (Int32)videoInitFrame.FramePerSecond;
                    byte[] spspps = videoInitFrame.Spspps.ToByteArray();

                    Console.WriteLine(spspps.Length);
                    if (recentWidth != width || recentHeight != height) {
                        FFMpegCodecDecInit(1, width, height);
                        recentWidth = width;
                        recentHeight = height;
                    }

                    FFMpegCodecDecExecute(1, spspps, spspps.Length);

                    Console.WriteLine("VideoInitFrame. uts=" + videoInitFrame.UtsMs);
                });
            registry.Add(new VideoFrame(), VideoFrame.Parser, (int)PacketType.VideoFrame,
                (IMessage message) => {
                    VideoFrame videoFrame = (VideoFrame)message;
                    Console.WriteLine("VideoFrame. uts=" + videoFrame.UtsMs);
                    
                    byte[] frame = videoFrame.Frame.ToByteArray();
                    Console.WriteLine(frame.Length);
                    if (recentWidth != 0 && recentHeight != 0) {
                        IntPtr RGB24 = FFMpegCodecDecExecute(1, frame, frame.Length);
                        int length = 1280 * 720 * 3 + 54;
                        if (RGB24 != IntPtr.Zero) {
                            lock (mList) {
                                mList.Add(new FrameData(1, RGB24, length));
                            }
                        }
                    }





                });
            registry.Add(new GenData(), GenData.Parser, (int)PacketType.GenData,
                (IMessage message) => {
                    GenData data = (GenData)message;
                    Console.WriteLine("GenData() called" + data.ToString());
                });
            registry.Add(new GenDataStartRequest(), GenDataStartRequest.Parser, (int)PacketType.GenDataStartRequest, null);
            registry.Add(new GenDataStartResponse(), GenDataStartResponse.Parser, (int)PacketType.GenDataStartResponse,
                (IMessage message) => {
                    GenDataStartResponse res = (GenDataStartResponse)message;
                });
            registry.Add(new GenDataStopRequest(), GenDataStopRequest.Parser, (int)PacketType.GenDataStopRequest, null);
            registry.Add(new GenDataStopResponse(), GenDataStopResponse.Parser, (int)PacketType.GenDataStopResponse,
                (IMessage message) => {
                    GenDataStopResponse res = (GenDataStopResponse)message;
                });
        }

        public static string getUUID() {
            string uuid = string.Empty;

            ManagementClass mc = new ManagementClass("Win32_ComputerSystemProduct");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc) {
                uuid = mo.Properties["UUID"].Value.ToString();
                break;
            }

            return uuid;
        }

    }
}