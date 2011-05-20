
module SnappyBird

    open System
    open System.IO
    open TweetSharp    
    open HtmlAgilityPack



    let service = new TwitterService()

    let urlencode (s:string) = 
        System.Web.HttpUtility.UrlEncode(s)

    let print_tweet (t:ITweetable) =
        printfn "%s\n%s\n" t.Author.ScreenName t.Text
        ()

    let print_trend (t:TwitterTrend) = 
        printfn "%s, %s, %s" t.Name t.Query t.RawSource
        ()

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

    let search (query:string)  = 
        let result = service.Search(query)
        result

    let get_tweets_for_screenname (sn:string)  = 
        let result = search("from:" + sn.Replace("@", "") )
        result.Statuses

    let get_tweets_with_entities (tweets:seq<ITweetable>) = 
        let filtered = 
            tweets 
            |> Seq.filter (fun t ->  (Seq.length t.Entities) > 0 )
        filtered

    let get_top_tweets () = 
        let tweets = get_tweets_for_screenname("toptweets")
        tweets

    let get_tweets_for_screennames sns = sns |> Seq.collect (fun sn -> get_tweets_for_screenname(sn))

    let get_tweets_for_searchterms ts = ts |> Seq.collect (fun q -> search(q).Statuses )

    let scrape_tweets sn = 
        let req = new System.Net.WebClient();
        let html  = req.DownloadString("http://twitter.com/" + sn);
        let doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(html) |> ignore
        let timeline = doc.DocumentNode.SelectSingleNode("//ol[@id='timeline']")
        let statusnodes = timeline.SelectNodes("//li[@id]")
        let status_ids = 
            statusnodes 
            |> Seq.filter (fun s -> s.Id.StartsWith("status_") ) 
            |> Seq.map (fun s -> Convert.ToInt64( s.Id.Replace("status_","") ) )
        let statuses = 
            status_ids
            |> Seq.map ( fun id -> service.GetTweet( id ) )
        statuses

    let as_tweetable ts = ts |> Seq.map ( fun t->  t :> ITweetable )


