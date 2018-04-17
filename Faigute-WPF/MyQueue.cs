using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faigute_WPF
{
    class MyQueue
    {
        //面部信息的四种情况
        static int YES = 3;
        static int MAYBE = 2;
        static int NO = 1;
        static int UNKNOW = 0;

        private int Maxlength = 0;
        private int[] datas;
        private int length = 0;

        public MyQueue(int length)
        {
            this.Maxlength = length;
            datas = new int[Maxlength];
        }

        public MyQueue()
        {
            this.length = 515 * 424;
        }

        public void Push(int data)
        {
            if (length == Maxlength)
            {
                for (int i = 0; i < Maxlength - 1; i++)
                {
                    datas[i] = datas[i + 1];
                }
                datas[Maxlength - 1] = data;
            }
            else
            {
                datas[length] = data;
                length++;
            }
        }

        public int CountYes()
        {
            int count = 0;
            for(int i=0;i<length;i++)
            {
                if (datas[i] == YES)
                    count++;
            }
            return count;
        }

        public int CountMaybe()
        {
            int count = 0;
            for (int i = 0; i < length; i++)
            {
                if (datas[i] == MAYBE)
                    count++;
            }
            return count;
        }

        public int CountNo()
        {
            int count = 0;
            for (int i = 0; i < length; i++)
            {
                if (datas[i] == NO)
                    count++;
            }
            return count;
        }

        public int CountUnknow()
        {
            int count = 0;
            for (int i = 0; i < length; i++)
            {
                if (datas[i] == UNKNOW)
                    count++;
            }
            return count;
        }

        public int getLength()
        {
            return this.length;
        }

        public int[] getData()
        {
            return this.datas;
        }

    }
}
