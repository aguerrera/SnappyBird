
module SnappyBird.Twitter

    open System
    open System.Collections.Generic;
    open System.IO
    open TweetSharp    
    open HtmlAgilityPack

    // this is a big hash of all the tweets.  useful for doing some ops
    let big_hash_of_tweets = new Dictionary<int64, TwitterSearchStatus>();

    let service = new TwitterService()

    // basic print_tweet function.  this can/should be updated
    let print_tweet (t:ITweetable) =
        printfn "%s\n%s\n" t.Author.ScreenName t.Text
        ()

    // basic print_trend function.
    let print_trend (t:TwitterTrend) = 
        printfn "%s, %s, %s" t.Name t.Query t.RawSource
        ()

    // convert your list a' into some ITweetables.
    let as_tweetable ts = ts |> Seq.map ( fun t->  t :> ITweetable )

    // meat and potatoes of this bad boy.
    // using the Twitter Search api b/c there is no (little?) rate limiting (?).
    // not an expert on this though.
    let search (query:string)  = 
        let result = service.Search(query)
        for tweet in result.Statuses do
            if not (big_hash_of_tweets.ContainsKey(tweet.Id)) then
                big_hash_of_tweets.[tweet.Id] <- tweet
        result

    // get tweets for a screen name
    let get_tweets_for_screenname (sn:string)  = 
        let result = search("from:" + sn.Replace("@", "") )
        result.Statuses

    // filter out your tweets with entities (urls).
    // pretty sure that there are search.api methods for this,
    // but it's just as relevant to filter out tweets that
    // we own.
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

    // scrape tweets from the actaul website.
    // use in case you get throttled.
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

    let print_via_screenname (tweets:seq<ITweetable>) (screen_name:string) = 
        tweets
            |> Seq.filter (fun t -> t.Author.ScreenName.Equals(screen_name, StringComparison.CurrentCultureIgnoreCase)  )
            |> Seq.iter print_tweet


    let print_tweets_from_screen_names (tweets:seq<ITweetable>) (screen_names:seq<string>) = 
        screen_names
            |> Seq.iter (fun sn ->
                            printfn "user: %s" sn
                            print_via_screenname tweets sn
                            ()
                        )


    let get_authors_from_tweets (tweets:seq<ITweetable>) =
        tweets
            |> Seq.map (fun t -> t.Author.ScreenName )
            |> Seq.distinct



    let get_urls_from_tweets (tweets:seq<ITweetable>) = 
            let tweetents = get_tweets_with_entities tweets |> Seq.toList
            tweetents 
            |> Seq.map ( fun t -> 
                            let us = t.Entities.Urls |> Seq.map (fun u -> u.Value )
                            (t.Author.ScreenName, us)
                        )
            |> Seq.toList  // get it in memory
            |> List.filter (fun (_, urls) ->  (Seq.length urls > 0) )  // filter only those that have urls
            |> List.map (fun (sn, urls) -> (sn, Seq.head urls))


