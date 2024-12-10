using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;
using Math = ExMath;
using Newtonsoft.Json;

public class FastPlayfairCipher : MonoBehaviour {

   public class ModSettingsJSON
   {
        public int CountdownTime;
        public string note;
   }

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMModSettings ModSettings;
   private static int ModuleIdCounter = 1;
   private int ModuleId;
   private bool ModuleSolved = false;
   public KMBombModule Module;
   public KMSelectable[] Buttons;
   public KMSelectable GoButton, SubmitButton;
   public TextMesh DisplayedMessage;
   public GameObject BarControl;
   public MeshRenderer Bar, GoBTN;
   private static readonly string[] DisplayedMessageCharacters = { "", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
   private bool LightsOn = false, PressedGo = false;
   private int NumberOfStages, CurrentStage = 1;
   private string input = "", answer = "";
   private int CharactersEntered = 0, threshold = 8;

   string Keyword = "";
   List<string> Alphabet = new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H", "I", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
   string AlphabetString = "";
   string PlayfairString = "";
   string[,] PlayfairMatrix = new string[5, 5];
   int Letter1Row, Letter1Column, Letter2Row, Letter2Column;

   void Awake()
   {
        
        ModuleId = ModuleIdCounter++;
        Module.OnActivate += Activate;
        
        GoButton.OnInteract += delegate ()
        {
            GoButtonHandle();
            return false;
        };
        SubmitButton.OnInteract += delegate ()
        {
            AnswerCheck();
            return false;
        };
        for (int i = 0; i < Buttons.Length; i++)
        {
            int j = i;
            Buttons[i].OnInteract += delegate ()
            {
                HandlePress(j);
                return false;
            };
        } 
   }
   void Activate()
   {
        Initialise();
        LightsOn = true;
   }
   void Initialise()
   {
        NumberOfStages = Random.Range(6, 10);
        Debug.LogFormat("[Fast Playfair Cipher #{0}] This module will have {1} stages.", ModuleId, NumberOfStages);
        threshold = FindThreshold();
        Debug.LogFormat("[Fast Playfair Cipher #{0}] Threshold time set to {1} seconds.", ModuleId, threshold);
        GenerateStage(1);
        PressedGo = false;
        CurrentStage = 1;
        input = "";
        CharactersEntered = 0;
        GoBTN.material.color = new Color32(229, 57, 53, 255);
        SubmitButton.GetComponent<MeshRenderer>().material.color = Color.gray;
        foreach (KMSelectable button in Buttons)
            button.GetComponent<MeshRenderer>().material.color = Color.gray;
   }
   void GenerateStage(int stage)
   {
        
        string DisplayedMessageCharacter1 = DisplayedMessageCharacters[Random.Range(0, DisplayedMessageCharacters.Length)];
        string DisplayedMessageCharacter2 = DisplayedMessageCharacters[Random.Range(0, DisplayedMessageCharacters.Length)];
        string message = DisplayedMessageCharacter1 + DisplayedMessageCharacter2;
        while (message == "" || message == "X" || message == "XX")
        {
            DisplayedMessageCharacter1 = DisplayedMessageCharacters[Random.Range(0, DisplayedMessageCharacters.Length)];
            DisplayedMessageCharacter2 = DisplayedMessageCharacters[Random.Range(0, DisplayedMessageCharacters.Length)];
            message = DisplayedMessageCharacter1 + DisplayedMessageCharacter2;
        }
        DisplayedMessage.text = message;
        Debug.LogFormat("[Fast Playfair Cipher #{0}] <Stage {1}> Displayed message is {2}", ModuleId, stage, message);
        if (DisplayedMessageCharacter1 == "J" || DisplayedMessageCharacter2 == "J")
            Debug.LogFormat("[Fast Playfair Cipher #{0}] <Stage {1}> J and I are interchangeable.", ModuleId, stage);

        if (DisplayedMessageCharacter1 == "J")
            DisplayedMessageCharacter1 = "I";
        if (DisplayedMessageCharacter2 == "J")
            DisplayedMessageCharacter2 = "I";
        if (DisplayedMessageCharacter1 == DisplayedMessageCharacter2)
        {
            DisplayedMessageCharacter2 = "X";
            Debug.LogFormat("[Fast Playfair Cipher #{0}] <Stage {1}> Both characters are the same, the second one becomes an X.", ModuleId, stage);
        }
        message = DisplayedMessageCharacter1 + DisplayedMessageCharacter2;
        if (message.Length == 1)
            message += "X";

        for (int i = 0; i < PlayfairMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < PlayfairMatrix.GetLength(1); j++)
            {
                if (message[0].ToString() == PlayfairMatrix[i, j])
                {
                    Letter1Row = i;
                    Letter1Column = j;
                }
                else if (message[1].ToString() == PlayfairMatrix[i, j])
                {
                    Letter2Row = i;
                    Letter2Column = j;
                }
            }
        }
        if (Letter1Row == Letter2Row)
        {
            if (Letter1Column == 0)
                Letter1Column = PlayfairMatrix.GetLength(1);
            else if (Letter2Column == 0)
                Letter2Column = PlayfairMatrix.GetLength(1);
            answer = PlayfairMatrix[Letter1Row, Letter1Column - 1] + PlayfairMatrix[Letter2Row, Letter2Column - 1];
            Debug.LogFormat("[Fast Playfair Cipher #{0}] <Stage {1}> After decryption the answer is {2}", ModuleId, stage, answer);
        }
        else if (Letter1Column == Letter2Column)
        {
            if (Letter1Row == 0)
                Letter1Row = PlayfairMatrix.GetLength(0);
            else if (Letter2Row == 0)
                Letter2Row = PlayfairMatrix.GetLength(0);
            answer = PlayfairMatrix[Letter1Row - 1, Letter1Column] + PlayfairMatrix[Letter2Row - 1, Letter2Column];
            Debug.LogFormat("[Fast Playfair Cipher #{0}] <Stage {1}> After decryption the answer is {2}", ModuleId, stage, answer);
        }
        else
        {
            answer = PlayfairMatrix[Letter1Row, Letter2Column] + PlayfairMatrix[Letter2Row, Letter1Column];
            Debug.LogFormat("[Fast Playfair Cipher #{0}] <Stage {1}> After decryption the answer is {2}", ModuleId, stage, answer);
        }
   }
   void Start()
   {
        Calculation();
   }
   IEnumerator Countdown()
   {
        float smooth = 16;
        float delta = 1f / (threshold * smooth);
        float current = 1f;
        for (int i = 1; i <= threshold * smooth; i++)
        {
            BarControl.gameObject.transform.localScale = new Vector3(1, 1, current);
            Bar.material.color = Color.Lerp(Color.red, Color.green, current);
            current -= delta;
            yield return new WaitForSeconds(1f / smooth);
        }
        BarControl.gameObject.transform.localScale = new Vector3(1, 1, 0f);
        HandleTimeout();
        yield return null;
   }
   void Calculation()
   {
        int BatteryHolders = Bomb.GetBatteryHolderCount();
        int PortPlatesCount = Bomb.GetPortPlateCount();
        IEnumerable<string[]> PortPlates = Bomb.GetPortPlates();
        int SerialNumberDigitsSum = Bomb.GetSerialNumberNumbers().Sum();
        int LitIndicatorsCount = Bomb.GetOnIndicators().Count();
        int UnlitIndicatorsCount = Bomb.GetOffIndicators().Count();
        int AABatteries = Bomb.GetBatteryCount(Battery.AA);
        string SerialNumber = Bomb.GetSerialNumber();
        int Batteries = Bomb.GetBatteryCount();
        int LastDigit = Bomb.GetSerialNumberNumbers().Last();
        int SerialNumberLettersCount = Bomb.GetSerialNumberLetters().Count();
        int ThirdCharacter = int.Parse(Bomb.GetSerialNumber()[2].ToString());
        int SixthCharacter = int.Parse(Bomb.GetSerialNumber()[5].ToString());
        int PortsCount = Bomb.GetPortCount();
        bool IsPrime = true;
        bool VowelInSerial = Bomb.GetSerialNumberLetters().Any(x => "AEIOU".Contains(x));
        if (PortsCount < 2)
            IsPrime = false;
        else for (int i = 2; i < PortsCount; i++)
        {
            if (PortsCount % i == 0)
            {
                IsPrime = false;
                break;
            }
        }
        int RCAPorts = Bomb.GetPortCount(Port.StereoRCA);
        int DVIPorts = Bomb.GetPortCount(Port.DVI);
        int DBatteries = Bomb.GetBatteryCount(Battery.D);
        int RJPorts = Bomb.GetPortCount(Port.RJ45);
        if (BatteryHolders + PortPlatesCount < 4)
            Keyword += "A";
        if (PortPlates.Any(x => x.Contains(Port.DVI.ToString()) && x.Contains(Port.RJ45.ToString()) && x.Contains(Port.PS2.ToString()) && x.Contains(Port.StereoRCA.ToString())))
            Keyword += "B";
        if (SerialNumberDigitsSum % 2 == 0)
            Keyword += "C";
        if (LitIndicatorsCount > UnlitIndicatorsCount)
            Keyword += "D";
        if (AABatteries == 4)
            Keyword += "E";
        if (PortPlatesCount < 2)
            Keyword += "F";
        if (VowelInSerial)
            Keyword += "G";
        if (UnlitIndicatorsCount > Batteries)
            Keyword += "H";
        if (LastDigit % 2 == 1)
            Keyword += "I";
        if (SerialNumberLettersCount < PortPlatesCount)
            Keyword += "K";
        if (ThirdCharacter % 2 == SixthCharacter % 2)
            Keyword += "L";
        if (PortPlates.Any(x => x.Contains(Port.Parallel.ToString()) && x.Contains(Port.Serial.ToString())))
            Keyword += "M";
        if (IsPrime)
            Keyword += "N";
        if (BatteryHolders == 3)
            Keyword += "O";
        if (RCAPorts == 0)
            Keyword += "P";
        if (DVIPorts > Batteries)
            Keyword += "Q";
        if (!VowelInSerial)
            Keyword += "R";
        if (PortPlatesCount > 1)
            Keyword += "S";
        if ("ZABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumberLetters().Skip(1).First()) % 2 == 1)
            Keyword += "T";
        if (DBatteries == 3)
            Keyword += "U";
        if (LastDigit % 2 == 0)
            Keyword += "V";
        if (RJPorts > 2)
            Keyword += "W";
        if ("ZABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(Bomb.GetSerialNumberLetters().First()) % 2 == 0)
            Keyword += "X";
        if (Batteries == 5)
            Keyword += "Y";
        if (SerialNumberDigitsSum > 17)
            Keyword += "Z";

        Debug.LogFormat("[Fast Playfair Cipher #{0}] Keyword: {1}", ModuleId, Keyword);
        for (int i = 0; i < Alphabet.Count; i++)
             if (Keyword.Contains(Alphabet.ElementAt(i)))
             {
                Alphabet.RemoveAt(i);
                i--;
             }
        for (int i = 0; i < Alphabet.Count; i++)
            AlphabetString += Alphabet.ElementAt(i).ToString();

        if (SerialNumber.Any(x => "PLAYFAIR".Contains(x)))
        {
           PlayfairString = Keyword + AlphabetString;
           Debug.LogFormat("[Fast Playfair Cipher #{0}] Serial number contains a character that is in the word PLAYFAIR, placing the alphabet after the keyword.", ModuleId);
        } 
        else
        {
           PlayfairString = AlphabetString + Keyword;
           Debug.LogFormat("[Fast Playfair Cipher #{0}] Serial number doesn't contain a character that is in the word PLAYFAIR, placing the alphabet before the keyword.", ModuleId);
        }
              
        for (int i = 0; i < PlayfairMatrix.GetLength(0); i++)
            for (int j = 0; j < PlayfairMatrix.GetLength(1); j++)
                PlayfairMatrix[i, j] = PlayfairString[5 * i + j].ToString();
        Debug.LogFormat("[Fast Playfair Cipher #{0}] Playfair matrix:", ModuleId);
        for (int i = 0; i < PlayfairMatrix.GetLength(0); i++)
            Debug.LogFormat("[Fast Playfair Cipher #{0}] {1}{2}{3}{4}{5}", ModuleId, PlayfairMatrix[i, 0], PlayfairMatrix[i, 1], PlayfairMatrix[i, 2], PlayfairMatrix[i, 3], PlayfairMatrix[i, 4]);
   }
   void GoButtonHandle()
   {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, GoButton.transform);
        GoButton.AddInteractionPunch();
        if (!LightsOn || ModuleSolved || PressedGo)
            return;
        GoBTN.GetComponent<MeshRenderer>().material.color = Color.gray;
        SubmitButton.GetComponent<MeshRenderer>().material.color = new Color32(229, 57, 53, 255);
        foreach (KMSelectable button in Buttons)
            button.GetComponent<MeshRenderer>().material.color = new Color32(229, 57, 53, 255);
        Debug.LogFormat("[Fast Playfair Cipher #{0}] Let's go!", ModuleId);
        StartCoroutine("Countdown");
        PressedGo = true;
   }
   void AnswerCheck()
   {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitButton.transform);
        SubmitButton.AddInteractionPunch();
        if (!LightsOn || ModuleSolved || CharactersEntered < 2 || !PressedGo)
            return;
        StopCoroutine("Countdown");
        Debug.LogFormat("[Fast Playfair Cipher #{0}] <Stage {1}> Entered: {2}", ModuleId, CurrentStage, input);
        if (input == answer)
        {
            Debug.LogFormat("[Fast Playfair Cipher #{0}] <Stage {1}> Cleared!", ModuleId, CurrentStage);
            CurrentStage++;
            if (CurrentStage > NumberOfStages)
            {
                Debug.LogFormat("[Fast Playfair Cipher #{0}] Module solved!", ModuleId);
                Audio.PlaySoundAtTransform("Disarmed", Module.transform);
                BarControl.gameObject.transform.localScale = new Vector3(1, 1, 0f);
                DisplayedMessage.text = "";
                foreach (KMSelectable button in Buttons)
                    button.GetComponent<MeshRenderer>().material.color = Color.gray;
                SubmitButton.GetComponent<MeshRenderer>().material.color = Color.gray;
                Module.HandlePass();
                ModuleSolved = true;
            }
            else
            {
                Audio.PlaySoundAtTransform("PassedStage", Module.transform);
                GenerateStage(CurrentStage);
                input = "";
                CharactersEntered = 0;
                StartCoroutine("Countdown");
            }
        }
        else
        {
            Debug.LogFormat("[Fast Playfair Cipher #{0}] Answer incorrect! Strike and reset!", ModuleId);
            Module.HandleStrike();
            Initialise();
        }
   }
   void HandlePress(int LetterIndex)
   {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[LetterIndex].transform);
        if (!LightsOn || ModuleSolved || CharactersEntered > 1 || !PressedGo)
            return;
        input += "ABCDEFGHIKLMNOPQRSTUVWXYZ"[LetterIndex].ToString();
        CharactersEntered++;
   }
   void HandleTimeout()
   {
        Debug.LogFormat("[Fast Playfair Cipher #{0}] Timeout! Strike and reset!", ModuleId);
        StopCoroutine("Countdown");
        Module.HandleStrike();
        Initialise();
   }
   int FindThreshold()
   {
        try
        {
            ModSettingsJSON settings = JsonConvert.DeserializeObject<ModSettingsJSON>(ModSettings.Settings);
            if (settings != null)
            {
                if (settings.CountdownTime < 8)
                    return 8;
                else if (settings.CountdownTime > 30)
                    return 30;
                else return settings.CountdownTime;
            }
            else return 8;
        }
        catch (JsonReaderException e)
        {
            Debug.LogFormat("[Fast Playfair Cipher #{0}] JSON failed with error {1}, using default threshold.", ModuleId, e.Message);
            return 8;
        }
   }
#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Press the GO! button with !{0} go. Submit your answer with !{0} submit xx (Must be two letters).";
#pragma warning restore 414
   IEnumerator ProcessTwitchCommand(string command) 
   {
        
        command = command.ToUpperInvariant().Trim();
        if (command.Equals("GO"))
        {
            GoButton.OnInteract();
            yield return null;
        }
        else if (Regex.IsMatch(command, @"^SUBMIT [A-Z][A-Z]$"))
        {
            command = command.Substring(7).Trim();
            if (command[0].ToString() == "J" || command[1].ToString() == "J")
            {
                yield return "sendtochaterror J and I are interchangeable!";
            }
            else
            {
                for (int index = 0; index < Buttons.Length; index++)
                {
                    if (command[0].ToString() == "ABCDEFGHIKLMNOPQRSTUVWXYZ"[index].ToString())
                    {
                        Buttons[index].OnInteract();
                        break;
                    }
                }
                for (int index = 0; index < Buttons.Length; index++)
                {
                    if (command[1].ToString() == "ABCDEFGHIKLMNOPQRSTUVWXYZ"[index].ToString())
                    {
                        Buttons[index].OnInteract();
                        break;
                    }
                }
                SubmitButton.OnInteract();
                yield return null;
            }
        }
        else
            yield return "sendtochaterror I don't understand!";
   }
}
