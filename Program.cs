using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;

class Server
{
    public static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 80);

        server.Start();
        Console.WriteLine("Server has started on 127.0.0.1:80.{0}Waiting for a connection...", Environment.NewLine);
        
        while (true)
        {
            TcpClient client = server.AcceptTcpClient();

            Console.WriteLine("A client connected.");

            NetworkStream stream = client.GetStream();

            while (client.Available < 3)
            {
                // wait for enough bytes to be available
            }

            Byte[] bytes = new Byte[client.Available];

            stream.Read(bytes, 0, bytes.Length);

            //translate bytes of request to string
            String data = Encoding.UTF8.GetString(bytes);

            if (new System.Text.RegularExpressions.Regex("^GET").IsMatch(data))
            {
                const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker

                Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                    + "Connection: Upgrade" + eol
                    + "Upgrade: websocket" + eol
                    + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                        System.Security.Cryptography.SHA1.Create().ComputeHash(
                            Encoding.UTF8.GetBytes(
                                new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                            )
                        )
                    ) + eol
                    + eol);

                stream.Write(response, 0, response.Length);
                while (true)
                {

                    byte[] buffer = new byte[200];
                    try
                    {
                        stream.Read(buffer, 0, buffer.Length);
                    }
                    catch
                    {
                        break;
                    }
                    int length = buffer[1] & 127;
                    int count = 0;
                
                    byte[] mask = new byte[4] { buffer[2], buffer[3], buffer[4], buffer[5] };

                    //decode
                    byte[] decoded = new byte[length];
                    for (int i = 6; i < length; i++)
                    {
                        decoded[count] = (byte)(buffer[i] ^ mask[count % 4]);
                        count++;
                    }

                    //test encode
                    byte[] content = Encoding.ASCII.GetBytes("hello");
                    byte[] response1 = new byte[6 + content.Length];
                    response1[0] = 129;
                    response1[1] = (byte)(128 | content.Length);
                    for (int i = 2; i < 6; i++)
                    {
                        response[i] = mask[i - 2];
                    }
                    count = 0;
                    for (int i = 6; i < response1.Length; i++)
                    {
                        response1[i] = (byte)(content[count] ^ mask[count % 4]);
                        count++;
                    }

                    //check msg from client
                    string s = Encoding.UTF8.GetString(decoded);
                    Console.WriteLine(s);

                    Array.Resize(ref buffer, length + 6);
                    stream.Write(buffer, 0, buffer.Length);
                
                }
            }

        }
       
    }
}