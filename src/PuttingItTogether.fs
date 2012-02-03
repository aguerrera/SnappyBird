module SnappyBird.PuttingItTogether

    open System
    open System.Collections.Generic;
    open System.IO
    open TweetSharp    

    let get_web_thumb url output_dir =                 
        let fn = SnappyBird.Output.get_hash_string_for_obj(url)
        let clean_url = SnappyBird.Twitter.clean_twitter_url url
        let ext = ".jpg"
        let path = output_dir + fn + ext
        printfn "getting thumb for %s" clean_url
        printfn "   saving to %s" path
        SnappyBird.TheInternet.get_website_bitmap_and_save path clean_url 1200 1200 700 700  
        ()


    let get_thumbs urls output_dir = 
        urls
        |> Seq.toList
        |> Seq.iter (fun u -> get_web_thumb u output_dir) 


    let parse_terms (searchbox:string) = 

        // so you can split by comma or newline
        let split_chars = 
            System.Environment.NewLine.ToCharArray()
            |> Array.append [| ',' |] 

        // split up the terms
        let search_terms = searchbox.Split (split_chars, StringSplitOptions.RemoveEmptyEntries) |> Array.toList

        // get the screennames only
        let screen_names = 
            search_terms 
            |> List.filter ( fun s -> s.StartsWith("@") ) 
            |> List.map (fun s-> s.Replace("@", "") )
            //|> List.append dudes_i_follow  // i can opptionally append dudes_i_follow to this. 
            |> Seq.distinct
            |> Seq.toList

        // filter the hashtags
        let hash_tags = search_terms |> List.filter ( fun s -> s.StartsWith("#") ) 

        // jam all the non screenname, non hash terms together
        let query_terms = search_terms |> List.filter ( fun s -> (not (s.StartsWith("#")) && not (s.StartsWith("@")))) 

        (screen_names, hash_tags, query_terms)


    // take a seq of tweets and flatten it out
    let merge_all_tweets (tlist:seq<seq<ITweetable>>) =
        let ats = seq { for t1 in tlist do
                        for t2 in t1 do
                            yield t2
                    }
        ats |> Seq.toList

    // flatten authors and the urls map into a list of (author, [urls])
    let map_authors_to_urls authors url_map = 
        let a_and_us = 
            authors
            |> List.map (fun a ->
                            let urlxs = 
                                    url_map
                                    |> List.filter (fun (sn, u) -> (sn = a))
                                    |> List.map (fun (_, u) -> u)
                            (a, urlxs)
                        ) 
        a_and_us

    // this one is actually a little tricky.  it takes our authors_and_urls and 
    // turns it into a map list of authors and their url infos.
    // what's a url info? it's a tuple of fullurl, page title, status [1/0], and original [ie shortened] url
    // i could put this async {} , but would it actually speed it up?  it's IO bound.
    let request_author_and_url_info_async (sn, urls) =                 
            printfn "getting author and url info. Long op!"
            async {
                let infos = 
                        urls 
                        |> List.map (fun u ->
                                            printfn "getting %s: url: %s" sn u
                                            SnappyBird.TheInternet.get_url_info(u)
                                            ) // warning!  IO bound action.
                return (sn, infos)
            }

    // run the job asyncronously not sure if this even works
    let get_author_url_infos a_and_us = 
        printfn "get authors and their infos.  asyncrhonously"
        let author_and_url_infos =                 
            a_and_us
            //|> Seq.take 1
            |> Seq.toList
            |> Seq.map request_author_and_url_info_async
            |> Async.Parallel 
            |> Async.RunSynchronously
            |> Seq.toList
        author_and_url_infos

    // this will get the distinct urls
    let get_distinct_urls_from_infos a_and_us = 
            let xs = seq { for sn, info in a_and_us do
                                for (url,title,status,shorturl) in info do
                                    if (status = 1) then yield url
                        }
            xs |> Seq.distinct |> Seq.toList

    // this is kind of random.
    let do_stuff_with_tweetents some_tweets = 
        // this does some filtering for tweets with entities (ie, hashtags/mentions/urls)
        let tweetents = SnappyBird.Twitter.get_tweets_with_entities some_tweets |> Seq.toList
        let hts = tweetents |> Seq.collect ( fun t -> t.Entities.HashTags ) |> Seq.toList
        let mentions = tweetents |> Seq.collect ( fun t -> t.Entities.Mentions ) |> Seq.toList
        let urls = tweetents |> Seq.collect ( fun t -> t.Entities.Urls ) |> Seq.toList

        printfn "entities: url data and stuff"
        tweetents |> Seq.iter (fun t -> printfn "%s\n%s\n\n" t.Author.ScreenName t.Text)
        hts |> Seq.iter (fun h -> printfn "%s" h.Text)
        urls |> Seq.iter (fun u -> printfn "%s" u.Value)
        mentions |> Seq.iter (fun m -> printfn "%A\t=>\t%s\n" m.Id m.ScreenName)
        ()
