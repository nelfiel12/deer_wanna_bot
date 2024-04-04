using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenCvSharp;

namespace deer_wanna_bot.Data
{
    internal class CaptureData
    {
        Mat m_templateMat;
        Mat m_maskMat;
        TemplateMatchModes m_mode = TemplateMatchModes.CCorrNormed;

        bool search(Mat src)
        {
            if(m_templateMat == null || m_templateMat.Empty())
                return false;

            if(m_maskMat == null || m_maskMat.Empty())
                return false;

            Mat resultMat = new Mat();
            Cv2.MatchTemplate(src, m_templateMat, resultMat, m_mode, m_maskMat);

            return false;
        }
    }
}
