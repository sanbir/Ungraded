using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace TestRunner
{
    [TestFixture]
    public class Tests
    {
        const string TempFile = @"D:\temp.dat";

        [Test]
        public void Download()
        {
            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService(@"D:\");
            IWebDriver driver = new FirefoxDriver(service);
            List<CsvEntry> csvEntries = new List<CsvEntry>();

            var tasks = new[] {"reading", "challenge"};
            for (int i = 1; i <= 11; i++)
            {
                foreach (var task in tasks)
                {
                    var zero = i < 10 ? "0" : "";

                    driver.Url = "https://www3.nd.edu/~pbui/teaching/cse.30331.fa16/" + task + zero + i.ToString() +"_tas.html";
                    driver.Navigate();

                    string aa = "/html/body/div/table[1]/tbody/tr[*]";
                    var items = driver.FindElements(By.XPath(aa));

                    foreach (var item in items.Where(it => it.Text.EndsWith("abiryuko")))
                    {
                        var studentNetId = item.Text.Split(' ')[0];

                        var csvEntry = new CsvEntry {StudentId = studentNetId, Task = task.Capitalize() + " " + zero + i.ToString() + " (R)"};
                        csvEntries.Add(csvEntry);

                        //var newLine = string.Format("{0},{1}", first, second);
                        //csv.AppendLine(newLine);
                    }
                }
            }

            SaveToFile(csvEntries);
        }

        [Test]
        public void Process()
        {
            var csv = new StringBuilder();
            var title =
                "Student Id,Reading 01 (R)	Reading 02 (R)	Reading 03 (R)	Reading 04 (R)	Reading 05 (R)	Reading 06 (R)	Reading 07 (R)	Reading 08 (R)	Reading 09 (R)	Reading 10 (R)	Reading 11 (R)	Challenge 01 (R)	Challenge 02 (R)	Challenge 03 (R)	Challenge 04 (R)	Challenge 05 (R)	Challenge 06 (R)	Challenge 07 (R)	Challenge 08 (R)	Challenge 09 (R)	Challenge 10 (R)	Challenge 11 (R)";
            title = title.Replace('\t', ',');
            string[] taskNames = title.Split(',');
            csv.AppendLine(title);

            List<CsvEntry> csvEntries = ReadFromFile();

            foreach (var student in csvEntries.GroupBy(c => c.StudentId))
            {
                var sb = new StringBuilder();
                sb.AppendFormat($"{student.Key},");
                foreach (var taskName in taskNames)
                {
                    if (taskName == "Student Id") continue;

                    bool has = student.Aggregate(false, (current, tasksForStudent) => current || (taskName == tasksForStudent.Task));

                    sb.AppendFormat(has ? "X," : ",");
                }

                csv.AppendLine(sb.ToString());
            }

            string name = DateTime.Now.Minute.ToString() +"-" +DateTime.Now.Second.ToString();
            File.WriteAllText(@"D:\result" + name + ".csv", csv.ToString());
        }

        [Test]
        public void CompareWithAll()
        {
            List<CsvEntry> csvEntries = ReadFromFile();
            List<CsvEntry> nonGradedCsvEntries = GetNonGradedStudents();
            List<CsvEntry> myNonGraded = (from nonGradedCsvEntry in nonGradedCsvEntries
                                          from csvEntry in csvEntries
                                          where csvEntry.StudentId == nonGradedCsvEntry.StudentId && csvEntry.Task == nonGradedCsvEntry.Task
                                          select csvEntry)
                                          .ToList();

            File.WriteAllText(@"D:\MyUngraded" + DateTime.Now.Minute + "-" + DateTime.Now.Second + ".csv", String.Join(",", myNonGraded));
        }

        List<CsvEntry> GetNonGradedStudents()
        {
            var title =
"Student Id,Reading 01 (R)	Reading 02 (R)	Reading 03 (R)	Reading 04 (R)	Reading 05 (R)	Reading 06 (R)	Reading 07 (R)	Reading 08 (R)	Reading 09 (R)	Reading 10 (R)	Reading 11 (R)	Challenge 01 (R)	Challenge 02 (R)	Challenge 03 (R)	Challenge 04 (R)	Challenge 05 (R)	Challenge 06 (R)	Challenge 07 (R)	Challenge 08 (R)	Challenge 09 (R)	Challenge 10 (R)	Challenge 11 (R)";
            title = title.Replace('\t', ',');
            string[] taskNames = title.Split(',');
            List<CsvEntry> nonGradedCsvEntries = new List<CsvEntry>();

            var reader = new StreamReader(File.OpenRead(@"D:\s.csv"));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                for (int i = 1; i < 23; i++)
                {
                    var value = values[i];

                    if (value == "" || value == "0")
                    {
                        nonGradedCsvEntries.Add(new CsvEntry { StudentId = values[0], Task = taskNames[i] });
                    }
                }
            }

            return nonGradedCsvEntries;
        }

        void SaveToFile(List<CsvEntry> csvEntries)
        {
            using (Stream stream = File.Open(TempFile, FileMode.Create))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                bformatter.Serialize(stream, csvEntries);
            }
        }

        List<CsvEntry> ReadFromFile()
        {
            using (Stream stream = File.Open(TempFile, FileMode.Open))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                return (List<CsvEntry>)bformatter.Deserialize(stream);
            }
        }
    }

    public static class Ext
    {
        public static string Capitalize(this string input)
        {
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
    }

    [Serializable]
    class CsvEntry
    {
         public string StudentId { get; set; }
        public string Task { get; set; }

        public override string ToString()
        {
            return new StringBuilder().AppendLine($"{StudentId},{Task}").ToString();
        }
    }
}
