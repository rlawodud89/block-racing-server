using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Network;

public class PlayerSession
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    public PlayerSession(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    public async Task StartAsync()
    {
        Console.WriteLine($"PlayerSession 시작 : {_client.Client.RemoteEndPoint}");

        await ReceiveLoopAsync();
    }

    private async Task ReceiveLoopAsync()
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int received = await _stream.ReadAsync(buffer);

                if (received == 0)
                    break;

                string message = Encoding.UTF8.GetString(buffer, 0, received);

                Console.WriteLine($"Receive : {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Disconnect();
        }
    }

    public async Task SendAsync(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        await _stream.WriteAsync(data);
    }

    private void Disconnect()
    {
        _stream.Close();
        _client.Close();
    }
}


