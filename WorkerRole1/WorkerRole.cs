using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Xml;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {        
        public override void Run()
        {
            // 這是背景工作實作範例。請用您的邏輯予以取代。
            Trace.TraceInformation("WorkerRole1 entry point called", "Information");
            
            while (true)
            {
                //Thread.Sleep(10000);
                //Trace.TraceInformation("Working", "Information");
                Get_opendata_info(1);
                Get_opendata_info(2);
                Get_opendata_info(3);
                Get_opendata_info(4);
                Get_opendata_info(5);
                Get_opendata_info(6);
                //Get_opendata_info(7);
                Get_opendata_info(8);
                Get_opendata_info(9);
                Get_machine_info();                
            }
        }

        public override bool OnStart()
        {
            // 設定同時連接的數目上限
            ServicePointManager.DefaultConnectionLimit = 12;

            // 如需有關處理組態變更的資訊，
            // 請參閱位於 http://go.microsoft.com/fwlink/?LinkId=166357 的 MSDN 主題。

            return base.OnStart();
        }

        public void Get_kmz_info()
        {
            string webAddr = "http://thb-gis.thb.gov.tw/Normaltemp.aspx?U=thb";

            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(webAddr);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();

            StreamReader stmReader = new StreamReader(response.GetResponseStream());

            string stringResult = stmReader.ReadToEnd();
        }

        public void Get_machine_info()
        {
            string webAddr = "http://sctek.cloudapp.net/Service1.svc/ReturnAirData";
            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(webAddr);
            HttpWebResponse resp = (HttpWebResponse)httpWebRequest.GetResponse();
            Stream resStream = resp.GetResponseStream();
            DataContractJsonSerializer obj = new DataContractJsonSerializer(typeof(Machine));
            Machine result = obj.ReadObject(resStream) as Machine;
            int smoke = 0;
            if (result.Smoke != null)
            {
                smoke = int.Parse(result.Smoke);
            }

            int liquidgas = 0;
            if (result.Liquidgas != null)
            {
                liquidgas = int.Parse(result.Liquidgas);
            }

            int naturalGas = 0;
            if (result.NaturalGas != null)
            {
                naturalGas = int.Parse(result.NaturalGas);
            }
            if (    smoke > 500
                ||  liquidgas > 500
                ||  naturalGas > 500)
            {
                Protect_DBDataContext DB = new Protect_DBDataContext();
                MachineInfo machine = DB.MachineInfo.First();
                webAddr = "http://protecttw.cloudapp.net/Service1.svc/machine_alarm/" + machine.Latitude + "/" + machine.Longitude + "/" + result.Liquidgas + "/" + Guid.NewGuid();
                httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(webAddr);
                HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();
            }
        }

        public void Get_opendata_info(int type)
        {
            Protect_DBDataContext DB = new Protect_DBDataContext();
            string webAddr = "";
            switch (type)
            {
                case 1:    // 地震
                    webAddr = "https://alerts.ncdr.nat.gov.tw/RssAtomFeed.ashx?AlertType=6";
                    break;
                case 2:    // 海嘯
                    webAddr = "https://alerts.ncdr.nat.gov.tw/RssAtomFeed.ashx?AlertType=7";
                    break;
                case 3:    // 豪大雨
                    webAddr = "https://alerts.ncdr.nat.gov.tw/RssAtomFeed.ashx?AlertType=10";
                    break;
                case 4:    // 颱風
                    webAddr = "https://alerts.ncdr.nat.gov.tw/RssAtomFeed.ashx?AlertType=5";
                    break;
                case 5:    // 水庫洩洪
                    webAddr = "https://alerts.ncdr.nat.gov.tw/RssAtomFeed.ashx?AlertType=12";
                    break;
                case 6:    // 淹水警戒
                    webAddr = "https://alerts.ncdr.nat.gov.tw/RssAtomFeed.ashx?AlertType=8";
                    break;
                case 7:    // 土石流
                    webAddr = "https://alerts.ncdr.nat.gov.tw/RssAtomFeed.ashx?AlertType=9";
                    break;
                case 8:    // 河川高水位
                    webAddr = "https://alerts.ncdr.nat.gov.tw/RssAtomFeed.ashx?AlertType=11";
                    break;
                case 9:    // 預警姓道路封閉
                    webAddr = "https://alerts.ncdr.nat.gov.tw/RssAtomFeed.ashx?AlertType=13";
                    break;
                default:
                    return;
            }
            
            HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(webAddr);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();

            StreamReader stmReader = new StreamReader(response.GetResponseStream());

            string stringResult = stmReader.ReadToEnd();

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(stringResult);

            XmlNamespaceManager xnm = new XmlNamespaceManager(xml.NameTable);
            xnm.AddNamespace("x", "http://www.w3.org/2005/Atom");
            string xpath = "/x:feed/x:entry";

            List<DisasterDB> data_array = new List<DisasterDB>();
            foreach (XmlNode xn in xml.SelectNodes(xpath, xnm))
            {
                DisasterDB data_info = new DisasterDB();
                foreach (XmlNode nodes in xn.ChildNodes)
                {
                    if (nodes.Name == "id")
                    {
                        data_info.DisasterDataID = nodes.InnerText;
                    }
                    else if (nodes.Name == "title")
                    {
                        data_info.DisasterTitle = nodes.InnerText;
                    }
                    else if (nodes.Name == "updated")
                    {
                        data_info.DisasterUpdate = DateTime.Parse(nodes.InnerText);
                    }
                    else if (nodes.Name == "summary")
                    {
                        data_info.DisasterSummary = nodes.InnerText;
                    }
                }

                var CheckData =
                    from opendata_info in DB.DisasterDB
                    where opendata_info.DisasterDataID == data_info.DisasterDataID && opendata_info.DisasterSummary == data_info.DisasterSummary
                    select opendata_info;
                if (CheckData.Count() == 0)
                {
                    data_array.Add(data_info);
                }
            }
            try
            {
                if (data_array.Count() > 0)
                {
                    DB.DisasterDB.InsertAllOnSubmit(data_array);
                    DB.SubmitChanges();
                }
            }
            catch
            {
            
            }
        }       
    }
}
