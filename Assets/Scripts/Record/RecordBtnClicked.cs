using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public class RecordBtnClicked : MonoBehaviour {
    public Text TextBox;
    public Text ResponseText;
    public Button RecordBtn;
    public RawImage RecordImg;
    public RawImage StopImg;

    struct ClipData
    {
            public int samples;
    }

    const int HEADER_SIZE = 44;

    private int minFreq;
    private int maxFreq;
    private string deviceName;
    private bool onRecord = false;

    private bool micConnected = false;

    //A handle to the attached AudioSource
    private AudioSource goAudioSource;

    public string apiKey = "AIzaSyCO0ZlLL7LjbtSCkzqGZNaDKzJgeQ1IkwI";
    // Use this for initialization
    void Start () {
        RecordImg.enabled = true;
        StopImg.enabled = false;
        //Check if there is at least one microphone connected
        if(Microphone.devices.Length <= 0)
        {
            //Throw a warning message at the console if there isn't
            Debug.LogWarning("Microphone not connected!");
        }
        else //At least one microphone is present
        {
            //Set 'micConnected' to true
            micConnected = true;
            deviceName = Microphone.devices[0];

            //Get the default microphone recording capabilities
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);

            //According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...
            if(minFreq == 0 && maxFreq == 0)
            {
                    //...meaning 44100 Hz can be used as the recording sampling rate
                    maxFreq = 44100;
            }

            //Get the attached AudioSource component
            goAudioSource = this.GetComponent<AudioSource>();
        }
    }

    public void BtnClicked() {
        if (onRecord) {
            StopRecord();
        } else {
            StartRecord();
        }
        onRecord = !onRecord;
    }

    void StartRecord()
    {
        RecordImg.enabled = false;
        StopImg.enabled = true;
        //Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource
        goAudioSource.clip = Microphone.Start( null, true, 7, maxFreq); //Currently set for a 7 second clip
    }

    void StopRecord()
    {
        RecordImg.enabled = true;
        StopImg.enabled = false;
        if(Microphone.IsRecording(null))
        {
            float filenameRand = UnityEngine.Random.Range (0.0f, 10.0f);

            string filename = "testing" + filenameRand;

            Microphone.End(null); //Stop the audio recording

            Debug.Log( "Recording Stopped");

            if (!filename.ToLower().EndsWith(".wav")) {
                filename += ".wav";
            }

            var filePath = Path.Combine("testing", filename);
            filePath = Path.Combine(Application.persistentDataPath, filePath);
            Debug.Log("Created filepath string: " + filePath);

            // Make sure directory exists if user is saving to sub dir.
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            SavWav.Save (filePath, goAudioSource.clip); //Save a temporary Wav File
            Debug.Log( "Saving @ " + filePath);
            string apiURL = "https://speech.googleapis.com/v1/speech:recognize?key=" + apiKey;
            string Response;

            Debug.Log( "Uploading " + filePath);
            Response = HttpUploadFile (apiURL, filePath, "file", "audio/wav; rate=44100");
            Debug.Log ("Response String: " +Response);

            try {
                var jsonresponse = SimpleJSON.JSON.Parse(Response);

                if (jsonresponse != null) {        
                    string resultString = jsonresponse ["result"] [0].ToString ();
                    var jsonResults = SimpleJSON.JSON.Parse (resultString);
					if (jsonResults == null || jsonResults["alternative"] == null) {
						TextBox.text = "No response...";
						Translate(TextBox.text);
						return;
					}
                    string transcripts = jsonResults ["alternative"] [0] ["transcript"].ToString ();

                    Debug.Log ("transcript string: " + transcripts );
                    TextBox.text = transcripts;

                } else {
                    TextBox.text = "No response...";
                }
            } catch(Exception ex) {
                Debug.Log(string.Format("Error uploading file error: {0}", ex));
                TextBox.text = "err: "+ ex;
            } finally {
                goAudioSource.Play(); //Playback the recorded audio

                //File.Delete(filePath); //Delete the Temporary Wav file
            }
        }
        Translate(TextBox.text);
    }

    void Translate(string Input) {
        //TODO: phan tich TextBox.text
        ResponseText.text = "I don't understand what you mean...";
    }

    void OnGUI() 
    {
        //If there is a microphone
        if(micConnected)
        {
            //If the audio from any microphone isn't being recorded
            if(Microphone.IsRecording(null))
            {
                TextBox.text = "Recording in progress...";
            }
        }
        else // No microphone
        {
            //Print a red "Microphone not connected!" message at the center of the screen
            GUI.contentColor = Color.red;
            GUI.Label(new Rect(Screen.width/2-100, Screen.height/2-25, 200, 50), "Microphone not connected!");
        }
    }

    public string HttpUploadFile(string url, string file, string paramName, string contentType) {
        Debug.Log(string.Format("Uploading {0} to {1}", file, url));
        HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
        wr.ContentType = "application/json";
		wr.Method = "POST";
        //wr.KeepAlive = true;
        //wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
		String base64=""; 
		try {
			Byte[] bytes = File.ReadAllBytes(file);
			base64 = Convert.ToBase64String(bytes);
				Debug.Log("----------test2");
			ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
			using (var streamWriter = new StreamWriter(wr.GetRequestStream()))
			{
				string json = "{\n" +
								"  \"config\": {\n" +
								"      \"encoding\": \"LINEAR16\",\n" +
								"      \"languageCode\": \"en-US\",\n" +
								"      \"maxAlternatives\": 1\n" +
								"  },\n" +
								"  \"audio\": {\n" +
								"    \"content\": \"" +base64+"\"\n" +
								"  }\n" +
								"}\n";
				Debug.Log(string.Format("Json: {0}", json));
				streamWriter.Write(json);
				Debug.Log("----------test3");
				streamWriter.Flush();
				Debug.Log("----------test4");
				streamWriter.Close();
			}

		} catch (Exception e) {
			Debug.LogWarning(string.Format("Error: {0}", e));
			return "Error";
		}
        
        WebResponse wresp = null;
        try {
			Debug.Log("----------test2");
            wresp = wr.GetResponse();
            Stream stream2 = wresp.GetResponseStream();
            StreamReader reader2 = new StreamReader(stream2);

            string responseString =  string.Format("{0}", reader2.ReadToEnd());
            Debug.Log("HTTP RESPONSE" +responseString);
            return responseString;

        } catch(Exception ex) {
            Debug.Log(string.Format("Error uploading file error: {0}", ex));
			Debug.Log("----------test2");
            if(wresp != null) {
                    wresp.Close();
                    wresp = null;
                    return "Error";
            }
        } finally {
            wr = null;
        }

        return "empty";
    }

    // Update is called once per frame
    void Update () {
        
    }

	public bool MyRemoteCertificateValidationCallback(System.Object sender,
    X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
		bool isOk = true;
		// If there are errors in the certificate chain,
		// look at each error to determine the cause.
		if (sslPolicyErrors != SslPolicyErrors.None) {
			for (int i=0; i<chain.ChainStatus.Length; i++) {
				if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown) {
					continue;
				}
				chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
				chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
				chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan (0, 1, 0);
				chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
				bool chainIsValid = chain.Build ((X509Certificate2)certificate);
				if (!chainIsValid) {
					isOk = false;
					break;
				}
			}
		}
		return isOk;
	}
}
