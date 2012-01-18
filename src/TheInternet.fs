﻿module SnappyBird.TheInternet

    open System
    open System.Collections.Generic;
    open System.Drawing
    open System.IO
    open System.Windows.Forms
    open HtmlAgilityPack

    let urlencode (s:string) = 
        System.Web.HttpUtility.UrlEncode(s)

    let get_url_info url = 
        try
            let req = System.Net.WebRequest.Create(new Uri(url))
            use resp = req.GetResponse()
            let respuri = resp.ResponseUri.AbsoluteUri
            use reader = new StreamReader(resp.GetResponseStream())
            let html = reader.ReadToEnd()
            let doc = new HtmlAgilityPack.HtmlDocument()
            doc.LoadHtml(html) |> ignore
            let titlenode = doc.DocumentNode.SelectSingleNode("//title")
            let title = 
                match titlenode with
                | null -> ""
                | _ -> titlenode.InnerText.Trim()
            
            (respuri, title, 1, url)
        with
            | :? System.Net.WebException as ex ->
                    let wr = ex.Response :?> System.Net.HttpWebResponse
                    (url, wr.StatusCode.ToString(), -1, "")

            | _ as ex -> (url, ex.ToString(), 0, url)

        
    let unshorten url = 
        try
            let req = System.Net.WebRequest.Create(new Uri(url))
            use resp = req.GetResponse()
            let respuri = resp.ResponseUri
            respuri.AbsoluteUri
        with
            | _ as ex -> "bad url=>" + url

    let is_shortened url = 
        let uri = new Uri(url)
        let arr = uri.Host.Split('.')
        let dmn = arr.[arr.Length - 2]
        dmn.Length < 5

    let get_uri url = 
        if is_shortened(url) then
            unshorten(url)
        else
            url


    let get_website_bitmap (url:string) width height = 
        use browser = new WebBrowser()
        browser.ScrollBarsEnabled <- false
        browser.Width <- width
        browser.Height <- height

        let bmp = new Bitmap(width, height)
        let get_bitmap = 
            browser.BringToFront()
            browser.DrawToBitmap(bmp, new Rectangle(0, 0, width, height))

        do browser.DocumentCompleted.Add(fun _-> get_bitmap)

        browser.Navigate(url) |> ignore
        while browser.ReadyState <> WebBrowserReadyState.Complete do 
            Application.DoEvents()
        bmp

    let get_website_bitmap_and_save path url width height =
        let bmp = get_website_bitmap url width height
        let dir = Path.GetDirectoryName(path)
        if not (Directory.Exists(dir)) then
            Directory.CreateDirectory(dir) |> ignore
        bmp.Save(path)
        ()