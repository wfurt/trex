using System;
using System.Xml;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace par
{
    public class History
        {
            public int round = 0;
            public int fail = 0;
            public int ok = 0;
            public int skip = 0;
        }
    class Program
    {
        static bool append = false;
        static String resultFile = "testHistory.xml";
        static bool debug = false;
        static bool summary = false;
        static bool skipped = true;
        static public String testdir = Directory.GetCurrentDirectory();

        static public History current = new History();
        static public Dictionary<string, int> assemblies = 
                    new Dictionary<string, int>();

        static void HandleAssembly(XmlNode assembly)
        {
            int failed = int.Parse(assembly.Attributes["failed"].Value);
            int ok = int.Parse(assembly.Attributes["passed"].Value);
            int skip = int.Parse(assembly.Attributes["skipped"].Value);


            //Console.WriteLine("Handling assembly {0}", assembly.Attributes["name"]);
            if (debug) 
            {
                Console.WriteLine("Handling assembly {3} : pass: {0} fail: {1} skip={2}", ok, failed, skip, assembly.Attributes["name"].Value);
            }
            current.ok += ok;
            current.fail += failed;
            current.skip += skip;

            if (failed == 0 && skip == 0)
            {
                return ;
            }
            foreach (XmlElement node in assembly.ChildNodes)
            {
                if (node.Name != "collection")
                {
                    continue;
                }

                foreach (XmlElement test in node.ChildNodes)
                {
                    String result = test.Attributes["result"].Value;
                    if (result != "Pass")
                    {
                        if (skipped && result == "Skip")
                        {
                            continue;
                        }
                        Console.Error.WriteLine("{0}: {1} in {2}", result, test.Attributes["name"].Value, assembly.Attributes["name"].Value);
                        if (assemblies.ContainsKey(assembly.Attributes["name"].Value))
                        {
                            assemblies[assembly.Attributes["name"].Value] += 1;
                        }
                        else
                        {
                            assemblies.Add(assembly.Attributes["name"].Value, 1);
                        }
                    }
                }
            }
        }

        static void CheckResult(String FileName)
        {
             XmlDocument doc = new XmlDocument();
             doc.Load(FileName);

            XmlElement root = doc.DocumentElement;
            foreach(XmlNode node in root.ChildNodes ){
                string text = node.InnerText; //or loop through its children as well
                if (node.Name == "assembly" && node.HasChildNodes && node.Attributes["name"] != null)
                {
                    HandleAssembly(node);
                }
            }
        }
        static void Main(string[] args)
        {

            for (int i = 0; i < args.Length; i++)
            {
                String arg = args[i];
                if (arg == "-a" || arg == "--append")
                {
                    append = true;

                }
                else if (arg == "-d" || arg == "--debug") 
                {
                    debug = true;
                }
                else if (arg == "-r" || arg == "--results") 
                {
                    i++;
                    resultFile = args[i];
                }
                else if (arg == "-s" || arg == "--summary") 
                {
                    summary = true;
                }
                else if (arg == "-t" || arg == "--testdir") 
                {
                    i++;
                    testdir = args[i];
                }

            }

            XmlSerializer xs = new XmlSerializer(typeof(History));

            if (append)
            {
                // real old results so we can append 
                using(var sr = new StreamReader(resultFile))
                {
                    current = (History)xs.Deserialize(sr);
                }
            }


            DirectoryInfo di = new DirectoryInfo(testdir);
            // look recursively for testResults.xml
            FileInfo[] directories = di.GetFiles("testResults.xml", SearchOption.AllDirectories);

            foreach (var file in directories)
            {
                CheckResult(file.FullName);
            }
            current.round += 1;

            if (summary)
            {
                Console.WriteLine("Round: {0}", current.round);
                Console.WriteLine("Total passed: {0}", current.ok);
                Console.WriteLine("Total failed: {0}", current.fail);
                Console.WriteLine("Total skipped {0}", current.skip);
                foreach (KeyValuePair<string, int> pair in assemblies)
                {
                    Console.WriteLine("{0}, {1}", pair.Key, pair.Value);
                }
            }

            TextWriter tw = new StreamWriter(resultFile);
            xs.Serialize(tw, current);
        }
    }
}
