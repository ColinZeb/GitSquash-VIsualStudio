﻿namespace CommentWriter
{
    using System;
    using System.IO;

    /// <summary>
    /// A program to write a appropriate comment for a rebase.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Executes the program.
        /// </summary>
        /// <param name="args">Arguments about the rebase.</param>
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                return;
            }

            string fileName = args[0];

            var tempCommentFileName = Environment.GetEnvironmentVariable("COMMENT_FILE_NAME");

            if (string.IsNullOrWhiteSpace(tempCommentFileName))
            {
                string[] lines = File.ReadAllLines(fileName);
                WriteCommentFile(fileName, lines);
            }
            else
            {
                string[] lines = File.ReadAllLines(tempCommentFileName);
                WriteCommentFile(fileName, lines);
            }
        }

        private static void WriteCommentFile(string fileName, string[] lines)
        {
            using (var writer = new StreamWriter(fileName))
            {
                writer.NewLine = "\n";

                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }
            }
        }
    }
}
