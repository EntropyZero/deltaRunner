using System.Collections;
using System.IO;

namespace EntropyZero.deltaRunner
{
    public class FileInfoComparer : IComparer
    {
        public int Compare(object x, object y)
        {
//            int iResult;

            FileInfo oFileX = (FileInfo)x;
            FileInfo oFileY = (FileInfo)y;

            return string.Compare(oFileX.Name, oFileY.Name);

//            if (oFileX.LastWriteTime == oFileY.LastWriteTime)
//            {
//                iResult = 0;
//            }
//            else
//                if (oFileX.Name > oFileY.Name)
//                {
//                    iResult = 1;
//                }
//                else
//                {
//                    iResult = -1;
//                }
//
//
//            return iResult;
        }

    } 
}
