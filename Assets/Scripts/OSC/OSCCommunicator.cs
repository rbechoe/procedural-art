using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpOSC;

public class OSCCommunicator : MonoBehaviour
{
    public bool push;
    public bool overload;

    UDPSender sender;
    UDPListener listener;

    public int quantity = 100;

    private void Start()
    {

        HandleOscPacket callback = delegate (OscPacket packet)
        {
            OscMessage messageReceived = (OscMessage)packet;
            string str = "Received a message! ";
            foreach (object o in messageReceived.Arguments)
            {
                str += "/" + o.ToString();
            }
            print(str);
        };

        listener = new UDPListener(55555, callback);
    }

    private void Update()
    {
        if (push)
        {
            push = false;

            if (!overload)
                SendMessage();
            else
                StartCoroutine(MsgOverload());
        }
    }

    void SendMessage()
    {
        sender = new UDPSender("10.3.4.205", 55555);
        OscMessage message = new OscMessage("/test/", "hello world van Ravi");
        sender.Send(message);
        print("Send message");
        sender.Close();
    }

    IEnumerator MsgOverload()
    {
        yield return new WaitForEndOfFrame();

        for (int i = 0; i < quantity; i++)
        {
            SendMessage();
        }
    }

    private void OnApplicationQuit()
    {
        listener.Close();
    }
}
