using System;

namespace ringba_test
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "https://ringba-test-html.s3-us-west-1.amazonaws.com/TestQuestions/output.txt";
            Statistics stat = new Statistics(url);
        }
    }
}
