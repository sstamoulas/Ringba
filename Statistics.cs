using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ringba_test
{
    public class Statistics
    {
        private const int AsciiLowerCaseLetterMinValue = 97;
        private const int AsciiConvertValue = 32;
        private const int MaxBytesToRead = 2000;

        private string url;
        private string tempFilePath;

        private SortedDictionary<char, int> letterFrequency;
        private int totalCapLetters = 0;
        private SortedDictionary<string, int> wordFrequency;
        private SortedDictionary<string, int> twoCharPrefixFrequency;

        public Statistics(string url)
        {
            letterFrequency = new SortedDictionary<char, int>();
            wordFrequency = new SortedDictionary<string, int>();
            twoCharPrefixFrequency = new SortedDictionary<string, int>();

            this.url = url;
            tempFilePath = Path.GetTempFileName();
            DownloadFile();
            ReadFile();
            DeleteFile();
            PrintStats();
        }

        private void DownloadFile()
        {
            using (HttpClient client = new HttpClient())
            {
                WebClient myWebClient = new WebClient();

                //Download the Web resource and save it into the temp filesystem folder.
                myWebClient.DownloadFile(url, tempFilePath);
            }
        }

        private void ReadFile()
        {
            using (StreamReader r = new StreamReader(this.tempFilePath))
            {
                StringBuilder constructedWord = new StringBuilder();

                int bytesRead = 0;

                string leftOverWord = "";
                while (!r.EndOfStream)
                {
                    char[] buffer = new char[MaxBytesToRead];
                    bytesRead = r.ReadBlock(buffer, 0, MaxBytesToRead);

                    string str = new string(buffer);

                    //Add a space at the beginning of each Capital Letter
                    var regex = new Regex(@"([A-Z])");
                    str = regex.Replace(str, " $1");

                    /*
                     * If the first character was capitalized then there will be
                     * a space, so we need to remove it
                     */
                    if (str[0] == ' ')
                        str = str.Substring(1);

                    var arr = str.Split(' ');

                    /*
                     * Keep track of the last and first word in case when
                     * reading the bytes the last word was incomplete
                     */
                    string lastWord = arr[arr.Length - 1];
                    string firstWord = arr[0];

                    //Update the frequency of words
                    for(int i = 1; i < arr.Length - 1; i++)
                    {
                        updateWordFrequency(arr[i]);
                    }

                    /*
                     * If the first letter of the first word is lowercase,
                     * then it must be incomplete, so concatenate the
                     * leftOverWord from the previous read
                     */
                    if (Convert.ToInt32(firstWord[0]) >= AsciiLowerCaseLetterMinValue) {
                        updateWordFrequency(leftOverWord + firstWord);
                    }
                    else
                    {
                        updateWordFrequency(firstWord);
                        if (!String.IsNullOrEmpty(leftOverWord))
                            updateWordFrequency(leftOverWord);
                    }

                    /*
                     * Store the last word from the read and concatenate if
                     * the first word is incomplete
                     */
                    leftOverWord = lastWord;

                    //If there are no more bytes to read then exit the loop
                    if (bytesRead != MaxBytesToRead)
                        break;
                }

                //Add the last word of the text in, since it was skipped
                updateWordFrequency(leftOverWord);
            }
        }

        private void updateLetterFrequency(char letter, int value)
        {
            //If the letter is upper-case we need to convert it to lower-case
            int asciiValue = Convert.ToInt32(letter);

            letter = asciiValue < AsciiLowerCaseLetterMinValue ?
                (char)(asciiValue + AsciiConvertValue) : (char)asciiValue;

            var doesExist = letterFrequency.ContainsKey(letter);

            if (doesExist)
            {
                letterFrequency[letter] = letterFrequency[letter] + value;
            }
            else
            {
                letterFrequency.Add(letter, value);
            }
        }

        private void updateWordFrequency(string word)
        {
            var doesExist = wordFrequency.ContainsKey(word);

            if (doesExist)
            {
                wordFrequency[word] = wordFrequency[word] + 1;
            }
            else
            {
                wordFrequency.Add(word, 1);
            }
        }

        private void updateTwoCharPrefixFrequency(string prefix, int value)
        {
            var doesExist = twoCharPrefixFrequency.ContainsKey(prefix);

            if (doesExist)
            {
                twoCharPrefixFrequency[prefix] = twoCharPrefixFrequency[prefix] + value;
            }
            else
            {
                twoCharPrefixFrequency.Add(prefix, value);
            }
        }

        private void DeleteFile()
        {
            File.Delete(tempFilePath);
        }

        private void getLetterFrequency()
        {
            foreach (var pair in wordFrequency)
            {
                char[] arr = pair.Key.ToCharArray();

                foreach (var letter in arr)
                {
                    updateLetterFrequency(letter, pair.Value);
                }
            }
        }

        private void getPrefixFrequency()
        {
            foreach (var pair in wordFrequency)
            {
                totalCapLetters += pair.Value;

                if (pair.Key.Length > 2)
                {
                    updateTwoCharPrefixFrequency(pair.Key.Substring(0, 2), pair.Value);
                }
            }
        }

        private void PrintStats()
        {
            Thread letterThread = new Thread(getLetterFrequency);
            Thread prefixThread = new Thread(getPrefixFrequency);

            letterThread.Start();
            prefixThread.Start();

            letterThread.Join();
            prefixThread.Join();

            Console.WriteLine("Letter Frequency: ");
            foreach (var pair in letterFrequency)
            {
                Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }

            Console.WriteLine("Total Capital Letters: {0}", totalCapLetters);

            var keyOfMaxValue = wordFrequency.Aggregate((x, y) =>
                x.Value > y.Value ? x : y).Key;
            Console.WriteLine("Most Common Word: {0}", keyOfMaxValue);
            Console.WriteLine("{0} Frequency: {1}", keyOfMaxValue,
            wordFrequency[keyOfMaxValue]);

            keyOfMaxValue = twoCharPrefixFrequency.Aggregate((x, y) =>
                x.Value > y.Value ? x : y).Key;
            Console.WriteLine("Most Common Two Letter Prefix: {0}", keyOfMaxValue);
            Console.WriteLine("{0} Frequency: {1}",
                keyOfMaxValue,
                twoCharPrefixFrequency[keyOfMaxValue]);
        }
    }
}
