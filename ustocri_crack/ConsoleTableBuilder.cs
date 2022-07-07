using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ustcori_crack
{
    public class ConsoleTableBuilder
    {
        public string[,] tableContent { get; private set; }
        private string space;
        private string spaceBeforeTable;
        private bool showTableLine = true;
        public ConsoleTableBuilder(string[,] tableContent,string space = "",string spaceBeforeTable="", bool showTableLine = true)
        {
            this.tableContent = tableContent;
            this.space = space;
            this.showTableLine = showTableLine;
            this.spaceBeforeTable = spaceBeforeTable;
        }
        public void print()
        {
            int line = tableContent.GetLength(0);
            int[] lineColumnCount = new int[line];
            for (int i = 0; i < line; i++)
            {
                lineColumnCount[i] = tableContent.GetLength(1);
            }
            int column = lineColumnCount.Max();
            int[] maxColumnLength = new int[column];
            for (int j = 0; j < column; j++)//horizontal
            {
                int[] columnLength = new int[line];
                for (int i = 0; i < line; i++)//vertical
                {
                    columnLength[i] = tableContent.GetLength(1) > j ? getStringLength(tableContent[i,j]) : 0;
                }
                maxColumnLength[j] = columnLength.Max();
            }

            int tableLineLength = 0;
            if (showTableLine)
            {
                Console.Write(spaceBeforeTable);
                tableLineLength = maxColumnLength.Sum() + (column - 1) * getStringLength(space) + 1;
                for (int i = 0; i < tableLineLength; i++)
                {
                    Console.Write("—");
                }
                Console.Write("\n");
            }

            for (int i = 0; i < line; i++)
            {
                Console.Write(spaceBeforeTable);
                for (int j = 0; j < column; j++)
                {
                    string str = "";
                    if (tableContent.GetLength(1) > j)
                    {
                        str = tableContent[i,j];
                    }
                    int length = getStringLength(str);
                    Console.Write(str);
                    for (int k = 0; k < maxColumnLength[j] - length; k++)
                    {
                        Console.Write(" ");
                    }
                    if (j < column - 1)
                    {
                        Console.Write(space);
                    }
                }
                Console.Write("\r\n");
            }
            if (showTableLine)
            {
                Console.Write(spaceBeforeTable);
                for (int i = 0; i < tableLineLength; i++)
                {
                    Console.Write("—");
                }
                Console.Write("\n");
            }
        }
        private int getStringLength(string str)
        {
            if (str.Equals(string.Empty))
                return 0;
            int strlen = 0;
            ASCIIEncoding strData = new ASCIIEncoding();
            //将字符串转换为ASCII编码的字节数字
            byte[] strBytes = strData.GetBytes(str);
            for (int i = 0; i <= strBytes.Length - 1; i++)
            {
                if (strBytes[i] == 63)  //中文都将编码为ASCII编码63,即"?"号
                    strlen++;
                strlen++;
            }

            foreach(char c in str.ToCharArray())
            {
                if (c == '△') strlen--;
            }
            return strlen;
        }
    }
}
