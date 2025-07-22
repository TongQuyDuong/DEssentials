using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace iGame.AllInSlime
{
    public static class CSVHelper
    {
        /// <summary>
        /// Gets the full path to a CSV file in the persistent data path.
        /// </summary>
        /// <param name="fileName">The name of the CSV file (e.g., "data.csv").</param>
        /// <returns>The full file path.</returns>
        public static string GetPersistentPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        /// <summary>
        /// Appends a string (treated as a new line/row) to the end of a CSV file.
        /// Creates the file and directory if they don't exist.
        /// </summary>
        /// <param name="filePath">The full path to the CSV file.</param>
        /// <param name="lineToAppend">The string content to append. Should be a single line (no internal \n without proper CSV escaping).</param>
        public static void AppendStringToCsv(string filePath, string lineToAppend)
        {
            try
            {
                // Ensure the directory exists
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.Log($"Created directory: {directoryPath}");
                }

                // Append the new line. File.AppendAllText handles creating the file if it doesn't exist.
                // It also adds a newline character if the file doesn't end with one,
                // but it's often good practice to ensure your lineToAppend ends how you want,
                // or use StreamWriter for more control.
                // For simple line appends, add a newline character yourself.
                File.AppendAllText(filePath, lineToAppend + "\n");
                // Debug.Log($"Appended to CSV: {filePath} -> {lineToAppend}");
            }
            catch (IOException ex)
            {
                Debug.LogError($"Error appending to CSV file '{filePath}': {ex.Message}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"An unexpected error occurred while appending to CSV '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Appends multiple lines/rows to the end of a CSV file using StreamWriter for better performance with many lines.
        /// Creates the file and directory if they don't exist.
        /// </summary>
        /// <param name="filePath">The full path to the CSV file.</param>
        /// <param name="linesToAppend">A list or array of strings, each representing a new row.</param>
        public static void AppendMultipleLinesToCsv(string filePath, IEnumerable<string> linesToAppend)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.Log($"Created directory: {directoryPath}");
                }

                // StreamWriter (true for append mode) is efficient for multiple writes.
                // The 'using' statement ensures the writer is properly disposed (closed).
                using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8)) // true for append
                {
                    foreach (string line in linesToAppend)
                    {
                        writer.WriteLine(line); // WriteLine automatically adds a newline character
                    }
                }
                // Debug.Log($"Appended multiple lines to CSV: {filePath}");
            }
            catch (IOException ex)
            {
                Debug.LogError($"Error appending multiple lines to CSV file '{filePath}': {ex.Message}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"An unexpected error occurred while appending multiple lines to CSV '{filePath}': {ex.Message}");
            }
        }


        /// <summary>
        /// Clears all content from a CSV file. If the file doesn't exist, it creates an empty one.
        /// </summary>
        /// <param name="filePath">The full path to the CSV file.</param>
        public static void ClearCsvContent(string filePath)
        {
            try
            {
                // Ensure the directory exists (WriteAllText might not create it)
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.Log($"Created directory: {directoryPath} while attempting to clear/create file.");
                }

                // Writing an empty string effectively clears the file or creates an empty one.
                File.WriteAllText(filePath, string.Empty);
                Debug.Log($"Cleared content of CSV (or created empty): {filePath}");
            }
            catch (IOException ex)
            {
                Debug.LogError($"Error clearing CSV file '{filePath}': {ex.Message}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"An unexpected error occurred while clearing CSV '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Reads all lines from a CSV file. For demonstration.
        /// </summary>
        /// <param name="filePath">The full path to the CSV file.</param>
        /// <returns>An array of strings, each representing a line, or null if an error occurs.</returns>
        public static string[] ReadAllLinesFromCsv(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return File.ReadAllLines(filePath);
                }
                else
                {
                    Debug.LogWarning($"File not found for reading: {filePath}");
                    return Array.Empty<string>(); // Return empty array if file doesn't exist
                }
            }
            catch (IOException ex)
            {
                Debug.LogError($"Error reading CSV file '{filePath}': {ex.Message}");
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"An unexpected error occurred while reading CSV '{filePath}': {ex.Message}");
                return null;
            }
        }
    }
}
