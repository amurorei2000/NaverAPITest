using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.IO;

[RequireComponent(typeof(AudioSource))]
public class RecordManager : MonoBehaviour
{
    public string[] micDevices;
    public Text myText;

    AudioClip voice;
    byte[] voiceToByte;

    void Start()
    {
        // ����ũ ��ġ �޾ƿ���
        micDevices = Microphone.devices;
    }

    public void StartRecording()
    {
        // ���� ����(�ִ� 7��, 22050 Hz)
        voice = Microphone.Start(micDevices[0], false, 7, 22050);
        print("���� ��...");
    }

    public void EndRecording()
    {
        // ����, ����ũ�� ���� ���̶��...
        if (Microphone.IsRecording(micDevices[0]))
        {
            // ����ũ ����
            Microphone.End(micDevices[0]);
            print("���� ����!");
        }

        if (voice != null)
        {
            // ���̽��� ������ float �迭 ���� �� ���̽� ���
            float[] voiceData = new float[voice.samples];
            voice.GetData(voiceData, 0);

            // float �����͸� short �����ͷ� ��ȯ(����)
            Int16[] voiceToShort = new short[voiceData.Length];

            for (int i = 0; i < voiceData.Length; i++)
            {
                voiceToShort[i] = Convert.ToInt16(voiceData[i] * 32767);
            }

            // short �����͸� byte �����ͷ� ��ȯ
            byte[] voiceToByte = new byte[voiceData.Length * 2];

            for (int i = 0; i < voiceData.Length; i++)
            {
                byte[] byteArr = new byte[2];
                byteArr = BitConverter.GetBytes(voiceToShort[i]);
                byteArr.CopyTo(voiceToByte, i * 2);
            }

            // ���� ����
            using (FileStream fs = new FileStream(Application.dataPath + "MyVoice.wav", FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Write(voiceToByte, 0, voiceToByte.Length);
                WriteHeader(fs, voice);
            }

            // ���� �б�
            using (FileStream fs = new FileStream(Application.dataPath + "MyVoice.wav", FileMode.Open, FileAccess.Read))
            {
                byte[] readData = new byte[fs.Length];

                fs.Read(readData, 0, readData.Length);
                StartCoroutine(SendVoiceData(readData));
            }

        }
    }

    IEnumerator SendVoiceData(byte[] voice)
    {
        WWWForm form = new WWWForm();
        UnityWebRequest req = UnityWebRequest.Post("https://naveropenapi.apigw.ntruss.com/recog/v1/stt?lang=Kor", form);

        // ��� �߰�
        req.SetRequestHeader("X-NCP-APIGW-API-KEY-ID", "5bercwtegt");
        req.SetRequestHeader("X-NCP-APIGW-API-KEY", "nyVkU9I9Fs7LtYen6Chvh0dQ2hOk6pu2FTqtxGKt");
        req.SetRequestHeader("Content-Type", "application/octet-stream");
        UploadHandler upload = new UploadHandlerRaw(voice);
        //upload.contentType = "application/octet-stream";

        // ���̽� �����͸� ���ε�
        req.uploadHandler = upload;

        // ��û �� �ٿ�ε� ���
        yield return req.SendWebRequest();

        if(req != null)
        {
            string receiveData = req.downloadHandler.text;
            print($"���� ���: {receiveData}");
            myText.text = receiveData;
        }
        // ��û ����
        req.Dispose();

    }

    void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 one = 1;

        byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        byte[] byteBlockAlign = BitConverter.GetBytes(blockAlign);
        fileStream.Write(byteBlockAlign, 0, 2);

        UInt16 bps = 16;
        byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);

        //fileStream.Close();
    }

   
}
