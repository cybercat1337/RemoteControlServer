using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

class RemoteControlServer
{
    static TcpListener server;
    static NetworkStream networkStream;

    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern byte VkKeyScan(char ch);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    const int MOUSEEVENTF_LEFTDOWN = 0x02;
    const int MOUSEEVENTF_LEFTUP = 0x04;
    const int MOUSEEVENTF_RIGHTDOWN = 0x08;
    const int MOUSEEVENTF_RIGHTUP = 0x10;

    const int SM_CXSCREEN = 0;
    const int SM_CYSCREEN = 1;

    static void Main(string[] args)
    {
        server = new TcpListener(IPAddress.Any, 5000);
        server.Start();
        Console.WriteLine("Server started on port 5000...");
        networkStream = server.AcceptTcpClient().GetStream();
        Console.WriteLine("Client connected...");

        Thread screenShareThread = new Thread(SendScreen);
        screenShareThread.Start();

        Thread receiveCommandsThread = new Thread(ReceiveCommands);
        receiveCommandsThread.Start();
    }

    static void SendScreen()
    {
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        while (true)
        {
            using (Bitmap bmp = new Bitmap(screenWidth, screenHeight))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                    byte[] buffer = ms.ToArray();
                    networkStream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
                    networkStream.Write(buffer, 0, buffer.Length);
                }
            }

            Thread.Sleep(100);
        }
    }

    static void ReceiveCommands()
    {
        byte[] buffer = new byte[256];
        while (true)
        {
            int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
            string command = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
            string[] commandParts = command.Split('|');
            switch (commandParts[0])
            {
                case "MOUSE":
                    int x = int.Parse(commandParts[1]);
                    int y = int.Parse(commandParts[2]);
                    SetCursorPos(x, y);
                    break;
                case "LEFTCLICK":
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;
                case "KEY":
                    char key = char.Parse(commandParts[1]);
                    byte vk = VkKeyScan(key);
                    keybd_event(vk, 0, 0, UIntPtr.Zero);
                    keybd_event(vk, 0, 2, UIntPtr.Zero);
                    break;
            }
        }
    }
}
