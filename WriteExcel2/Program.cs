﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using ExcelApp = Microsoft.Office.Interop.Excel;
using System.IO;


namespace ReadExcel2
{
    class Program
    {
        static string OpenFileLocate(string path)
        {
            string d = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            path = Path.Combine(d, path);
            return path;
        }
        static string GetInfo(string location) {
            location = OpenFileLocate(location);
            string[] texts = File.ReadAllLines(location);
            string[] data = {""};
            foreach (string text in texts)
            {
                data = text.Split(',');
            }
            location = data[2];
            return location;
        }
        static string ConvMestoText(string messages) {
            string text = "";
            foreach (char message in messages)
            {
                if (Convert.ToInt32(message) == 10)
                {
                    text = text + ",";
                }
                else
                {
                    text = text + message;
                }
            }
            return text;
        }
        static bool CompDateTime(string date,int date2) {
            bool result = false;
            int c = date2, count = 0;
            string text = "";
            DateTime dts = DateTime.FromOADate(c);
            foreach (char dt in Convert.ToString(dts)) //remove time
            {
                if (Convert.ToInt32(dt) == 32)
                {
                    break;
                }
                text = text + dt;
            }
            count = text.Length;
            if (count == 8) {
                text = $"{text[4]}{text[5]}{text[6]}{text[7]}0{text[0]}0{text[2]}"; // d/m/yyy >> yyyymmdd
            }
            else if (count == 9) {
                if (text[1] == 47)
                {
                    text = $"{text[5]}{text[6]}{text[7]}{text[8]}0{text[0]}{text[2]}{text[3]}";  // d/mm/yyy >>yyyymmdd
                }
                else { 
                    text = $"{text[5]}{text[6]}{text[7]}{text[8]}{text[0]}{text[1]}0{text[3]}";  // dd/m/yyy >>yyyymmdd
                }
            }
            else {
                text = $"{text[6]}{text[7]}{text[8]}{text[9]}{text[0]}{text[1]}{text[3]}{text[4]}";  // dd/m/yyy >>yyyymmdd
            }
            Console.WriteLine($"excel date is >> {text} : Notepad date is >> {date} : Comparing... ");
            if (text.Equals(date))
            {
                result = true;
            }
            Console.WriteLine($"Dates are same : {result}");
            return result;
        }
        static string[] ReadExcelFile(string path, int sheet) // input = location of excel, output = array of data
        {
            string[] arrs;
            string message = "";
            string date = GetInfo(@"ChecklistGen\input\input.txt");
            path = OpenFileLocate(path);
            Application excelApp = new Application();
            if (excelApp == null)
            {
                Console.WriteLine("Excel is not installed!!");
            }
            ExcelApp.Workbook excelBook = excelApp.Workbooks.Open(path);
            ExcelApp._Worksheet excelSheet = excelBook.Sheets[sheet];
            ExcelApp.Range excelRange = excelSheet.UsedRange;
            int rows = excelRange.Rows.Count;
            int check = 0, start = 0, end = rows;
            while (check == 0) // find start point
            {
                Int32 c = Convert.ToInt32(excelRange.Cells[end, 1].Value2.ToString());
                if (CompDateTime(date, c) is false) {
                    check = end+1;
                    break;
                }
                end--;
            }
            arrs = new string[rows-check+1];
            Console.WriteLine($"Using data from row {check} to {rows}\n");
            for (int i = check; i <= rows; i++) {
                message = excelRange.Cells[i, 7].Value2.ToString();
                arrs[start] = message.ToUpper();
                start += 1;
            }
            excelApp.Quit();
            System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
            return arrs;
        }
        static void PrintArr(string[] arrs)
        { 
            string[] texts = arrs;
            foreach (string text in texts)
            {
                Console.WriteLine("Collected >> "+ConvMestoText(text));
            }
        }
        static int FindNumOfRows(string[] arrs) {
            int count = arrs.Length,numPo = 0,numMu = 0;
            for (int i = 0; i < count; i++) {
                if (arrs[i].Contains("RE-TOPOLOGY") || arrs[i].Contains("SPLIT RING") || arrs[i].Contains("CLOSE LOOP"))
                {
                    numPo = numPo + 1;
                    
                }
                else {
                    string data = ConvMestoText(arrs[i]);
                    string[] texts = data.Split(',');
                    foreach (string text in texts) {
                        numMu = numMu + 1;
                    }
                    numMu -= 1;
                }
            }
            int total = numPo + numMu;
            return total;
        }
        static string[,] ConvTo2DimArr(string[] arrs) {
            int count = arrs.Length, srtArr = 0;
            string[,] output = new string[FindNumOfRows(arrs),2];
            for (int i = 0; i < count; i++)
            {
                if (arrs[i].Contains("RE-TOPOLOGY") || arrs[i].Contains("SPLIT RING") || arrs[i].Contains("CLOSE LOOP"))
                {
                }
                else
                {
                    string data = ConvMestoText(arrs[i]);
                    string[] texts = data.Split(',');
                    for (int j = 0; j < texts.Length - 1; j++)
                    {
                        output[srtArr, 0] = texts[j];
                        output[srtArr, 1] = texts[texts.Length - 1].Trim('(',')');
                        srtArr++;
                    }
                }
            }
            return output;
        }
        static void EditData(string path, int page,string[,] arrs,string date) // input location,sheet,2dimension array >> output write to excel
        {
            path = OpenFileLocate(path);
            ExcelApp.Application excelBook = new ExcelApp.Application();
            ExcelApp.Workbook excelSheet = excelBook.Workbooks.Open(path);
            ExcelApp.Worksheet x = excelSheet.Sheets[page];
            int count = arrs.Length / 2 + 2, num = 0;
            for (int i = 2; i < count; i++) {
                x.Cells[i, 1] = num+1;
                x.Cells[i, 2] = arrs[num,0];
                x.Cells[i, 4] = arrs[num, 1];
                x.Cells[i, 5] = $"{date[4]}{date[5]}/{date[6]}{date[7]}/{date[0]}{date[1]}{date[2]}{date[3]}";
                num++;
                
            }
            excelSheet.Close(true, Type.Missing, Type.Missing);
            excelBook.Quit();
            
        }
        static void Main(string[] args)
        {
            // get date and file location
            string date = GetInfo(@"ChecklistGen\input\input.txt");
            Console.WriteLine("Date >> " + date);
            string bcfilename = $"broadcast{date}.xlsx"; 
            Console.WriteLine("Open file name >> "+bcfilename);
            string path = Path.Combine(@"ChecklistGen\", bcfilename);
            Console.WriteLine("File location >> "+path);
            // get data from excel
            Console.WriteLine("-------------------------------------");
            string[] arrs = ReadExcelFile(path, 1);
            Console.WriteLine("Collected data from excel");
            Console.WriteLine("-------------------------------------");
            PrintArr(arrs);  //
            int numOfrow = FindNumOfRows(arrs);
            //Push data to new one
            //find number of rows
            // put data to comfort format
            string[,] dataset = ConvTo2DimArr(arrs);
            string clfilename = $"check list_TUC ATN interface expansion_{date}.xlsx";
            Console.WriteLine("Writing to file >> " + clfilename);
            string filelocation = Path.Combine(@"ChecklistGen\", clfilename);
            EditData(filelocation, 1,dataset,date);
            Console.ReadKey();
        }
    }
}