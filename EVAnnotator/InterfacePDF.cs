using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;

namespace GenieSupervisor
{
    public class InterfacePDF
    {
        private int marginLeft, marginRight, marginTop, marginBottom;
        private string strFileName;

        public double screenLeft, screenTop, screenRight, screenBottom;

        Document doc;
        int pdfPageNo;
        public float[] colWidth;
        PdfWriter writer;
        public void InitSettings(int mLeft, int mRight, int mTop, int mBottom, string fileName)
        {
            this.marginLeft = mLeft;
            this.marginRight = mRight;
            this.marginTop = mTop;
            this.marginBottom = mBottom;
            this.strFileName = fileName + ".pdf";
            pdfPageNo = 1;
        }

        public void InitPdf()
        {
            if (doc != null)
                doc = null;
            doc = new Document(iTextSharp.text.PageSize.A4, marginLeft, marginRight, marginTop, marginBottom);
            writer = PdfWriter.GetInstance(doc, new FileStream(strFileName, FileMode.Create));
                //writer.PageEvent = new PDFFooter();

            OpenFile();
        }

        public void OpenFile()
        {
            doc.Open();
        }

        public void CloseFile()
        {
            doc.Close();
        }

        public void AppendTextHeading(string strTextToWrite, bool mainHeading = true)
        {
            iTextSharp.text.Font verdana;
            if (mainHeading)
                verdana = FontFactory.GetFont("Verdana", 14, iTextSharp.text.Font.NORMAL);
            else
                verdana = FontFactory.GetFont("Verdana", 12, iTextSharp.text.Font.NORMAL);

            Paragraph paragraph = new Paragraph(strTextToWrite, verdana);
            paragraph.Alignment = Element.ALIGN_CENTER;
            doc.Add(paragraph);
        }

        public void AppendText(string strTextToWrite)
        {
            iTextSharp.text.Font verdana = FontFactory.GetFont("Verdana", 10, iTextSharp.text.Font.NORMAL);
            Paragraph paragraph = new Paragraph(strTextToWrite, verdana);
            paragraph.Alignment = Element.ALIGN_LEFT;
            doc.Add(paragraph);
        }

        public void AppendBlankText(string strTextToWrite)
        {
            iTextSharp.text.Font verdana = FontFactory.GetFont("Verdana", 10, iTextSharp.text.Font.NORMAL);
            Paragraph paragraph = new Paragraph(strTextToWrite, verdana);
            paragraph.Alignment = Element.ALIGN_CENTER;
            doc.Add(paragraph);
        }

        public void AppendDateString(string[] strTextToWrite)
        {
            iTextSharp.text.Font verdana = FontFactory.GetFont("Verdana", 10, iTextSharp.text.Font.NORMAL);
            Chunk glue = new Chunk(new VerticalPositionMark());
            Phrase phrase = new Phrase();
            Paragraph paragraph = new Paragraph("", verdana);
            phrase.Add(new Chunk(strTextToWrite[0])); // Here I add projectname as a chunk into Phrase.    
            phrase.Add(glue); // Here I add special chunk to the same phrase.    
            phrase.Add(strTextToWrite[1]); // Here I add date as a chunk into same phrase.    
            paragraph.Add(phrase);
            doc.Add(paragraph);
        }

        public void AppendLine()
        {
            Paragraph p = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator()));
            doc.Add(p);
        }

        public void AppendTableHeader(string[] strTableHeader, float[] widths, float BorderWidth = 0.5f, int nHorzontalAlignment = 1)
        {
            colWidth = widths;
            PdfPTable table = new PdfPTable(strTableHeader.Length);
            table.SpacingBefore = 5f;
            //table.SpacingAfter = 30f;
            table.WidthPercentage = 100;
            table.SetWidths(widths);
            iTextSharp.text.Font cellFont = FontFactory.GetFont("Verdana", 9, iTextSharp.text.Font.BOLD);
            table.DefaultCell.HorizontalAlignment = nHorzontalAlignment;
            table.DefaultCell.VerticalAlignment = 1;
            table.DefaultCell.MinimumHeight = 15;
            table.DefaultCell.BorderWidth = BorderWidth;
            foreach (string item in strTableHeader)
                table.AddCell(new Phrase(item.ToString(), cellFont));
            doc.Add(table);
        }

        public void AppendTableRows(List<string> listTableContent, float BorderWidth = 0.5f, float minHeight = 10)
        {
            PdfPTable table = new PdfPTable(listTableContent.Count);
            table.WidthPercentage = 100;
            table.DefaultCell.Border = iTextSharp.text.Rectangle.RECTANGLE;
            table.DefaultCell.BorderWidth = BorderWidth;
            table.SetWidths(colWidth);
            iTextSharp.text.Font cellFont = FontFactory.GetFont("Verdana", 9, iTextSharp.text.Font.NORMAL);
            //table.DefaultCell.HorizontalAlignment = 1;
            table.DefaultCell.VerticalAlignment = 1;
            table.DefaultCell.MinimumHeight = minHeight;
            foreach (string item in listTableContent)
                table.AddCell(new Phrase(item.ToString(), cellFont));
            doc.Add(table);
        }

        public void AppendChartImage(System.Drawing.Image img)
        {
            iTextSharp.text.Image pic = iTextSharp.text.Image.GetInstance(img, System.Drawing.Imaging.ImageFormat.Jpeg);
            doc.Add(pic);
        }
    }
    public class PDFFooter : PdfPageEventHelper
    {
        public override void OnStartPage(PdfWriter writer, Document document)
        {
            //pdfInterface.AppendTableHeader();
            base.OnEndPage(writer, document);
            if (document.PageNumber == 1)
                return;
        }
    }
}


