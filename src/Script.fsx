#r @"..\packages\Hammock.1.2.3\lib\net40\Hammock.dll"
#r @"..\packages\TweetSharp.2.0.6\lib\4.0\Hammock.ClientProfile.dll"
#r @"..\packages\TweetSharp.2.0.6\lib\4.0\TweetSharp.dll"
#r @"..\packages\HtmlAgilityPack.1.4.0\lib\HtmlAgilityPack.dll"
#r @"..\packages\TweetSharp.2.0.6\lib\4.0\Newtonsoft.Json.dll"

#load "Twitter.fs"

open System
open System.IO
open System.Collections.Generic
open System.Web
open System.Net
open TweetSharp
open HtmlAgilityPack
open Newtonsoft.Json.Linq


(*
1. get friends of friends of friends -> subequent generations
2. determine url quality
3. sentiment analysis  http://intridea.com/tag/analysis
4. do google/youtube searches
5. do stock lookups
6. handle RTs?  Weight?
7. pre populate with toptweets, trends, hastags, or own friends, or other sources
8. output to static html and csv files 
9. use FsCharting for chart output

.. semantics
http://www.alchemyapi.com/tools/
http://text-processing.com/docs/
http://developer.zemanta.com/
amazon affiliates

f#charting
http://blogs.msdn.com/b/fsharpteam/archive/2011/04/15/getting-started-with-fsharpchart.aspx

*)



let print_trend = SnappyBird.print_trend
let print_tweet = SnappyBird.print_tweet


let toptweets = SnappyBird.get_top_tweets()
toptweets |> Seq.iter (fun t -> print_tweet t)

let current_trends = SnappyBird.service.ListCurrentTrends()
current_trends |> Seq.iter print_trend

let daily_trends = SnappyBird.service.ListDailyTrends()
daily_trends |> Seq.iter print_trend

let weekly_trends = SnappyBird.service.ListWeeklyTrends()
weekly_trends |> Seq.iter print_trend

let searchbox = @"
@gruber
@kottke
@diveintomark
@codinghorror
@danieltosh
@sportsguy33
"
let search_terms = searchbox.Split ( System.Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries) |> Array.toList
let screen_names = search_terms |> List.filter ( fun s -> s.StartsWith("@") ) |> List.map (fun s-> s.Replace("@", "") )
let trend_terms = daily_trends
                    |> Seq.map( fun t -> t.Name)
                    |> Seq.distinct
                    |> Seq.toList

let h1 = trend_terms
            |> Seq.toList
            |> List.filter ( fun t -> t.StartsWith("#") ) 
let h2 = search_terms |> List.filter ( fun s -> s.StartsWith("#") ) 
let hash_tags = List.append h1 h2

let q1 = trend_terms
            |> Seq.toList
            |> List.filter ( fun t -> not (t.StartsWith("#")) ) 
let q2 = search_terms |> List.filter ( fun s -> (not (s.StartsWith("#")) && not (s.StartsWith("@")))) 
let query_terms = List.append q1 q2


let tweets_from_screennames = SnappyBird.get_tweets_for_screennames(screen_names) |> SnappyBird.as_tweetable |> Seq.toList
let tweets_from_hashtags = SnappyBird.get_tweets_for_searchterms(hash_tags) |> SnappyBird.as_tweetable |> Seq.toList
let tweets_from_otherterms = SnappyBird.get_tweets_for_searchterms(query_terms) |> SnappyBird.as_tweetable |> Seq.toList


screen_names
    |> Seq.iter (fun sn ->
                    printfn "user: %s" sn
                    tweets_from_screennames
                        |> List.filter (fun t -> t.Author.ScreenName = sn)
                        |> List.iter print_tweet
                    ()
                )

tweets_from_screennames
    |> List.filter (fun t -> t.Author.ScreenName = "diveintomark")
    |> List.iter print_tweet

let xs = SnappyBird.get_tweets_for_screenname("toptweets") |> SnappyBird.as_tweetable |> Seq.toList

let all_tweets = 
    tweets_from_screennames
    |> Seq.append xs 
    //|> Seq.append tweets_from_hashtags
    //|> Seq.append tweets_from_otherterms
    |> Seq.toList

let tweetents = SnappyBird.get_tweets_with_entities all_tweets |> Seq.toList

let authors = all_tweets
                |> List.map (fun t -> t.Author.ScreenName )
                |> Seq.distinct
                |> Seq.toList

authors |> List.sort |> List.iter (fun s -> printfn "%s" s)

let urls2 = 
        tweetents 
        |> Seq.map ( fun t -> 
                        let us = t.Entities.Urls |> Seq.map (fun u -> u.Value )
                        (t.Author.ScreenName, us)
                    )
        |> Seq.toList  // get it in memory
        |> List.filter (fun (_, urls) ->  (Seq.length urls > 0) )  // filter only those that have urls
        |> List.map (fun (sn, urls) -> (sn, Seq.head urls))

urls2
    |> List.iter ( fun (sn, u) -> printfn "%s\t%s" sn u)  // print out the screenname/url

let authors_and_urls = 
    authors
    |> List.map (fun a ->
                    let infos = 
                            urls2
                            |> List.filter (fun (sn, u) -> (sn = a))
                            |> List.map (fun (_, u) -> u)
                    (a, infos)
                )

let mapped_urls =                 
    authors_and_urls
        |> List.map ( fun (sn, urls) -> 
                            let infos = 
                                    urls 
                                    |> List.map (fun u -> SnappyBird.get_url_info(u))
                            (sn, infos)
                    )  

mapped_urls
        |> List.iter (fun (sn, infos) -> 
                            infos |> 
                                List.iter (fun (u, t, s, _) -> printfn "%s\t%s\t%s" sn u t)
                            ()
                      )  


//for hashtags/mentions/urls
let hts = 
        tweetents |> Seq.collect ( fun t -> t.Entities.HashTags )
let mentions = 
        tweetents |> Seq.collect ( fun t -> t.Entities.Mentions )
let urls = 
        tweetents |> Seq.collect ( fun t -> t.Entities.Urls )
        

tweetents |> Seq.iter (fun t -> printfn "%s\n%s\n\n" t.Author.ScreenName t.Text)
hts |> Seq.iter (fun h -> printfn "%s" h.Text)
urls |> Seq.iter (fun u -> printfn "%s" u.Value)
mentions |> Seq.iter (fun m -> printfn "%A\t=>\t%s\n" m.Id m.ScreenName)

let urlinfos = urls 
                |> Seq.map (fun u -> u.Value)
                |> Seq.map (fun u -> SnappyBird.get_url_info(u))

urlinfos |> Seq.iter (fun (u, t, s, o) -> printfn "%s\t%s\t%A" u t s)

let rawurls = urls 
                |> Seq.map (fun u -> u.Value)
                |> Seq.map (fun u -> SnappyBird.get_uri(u))
rawurls |> Seq.iter (fun u -> printfn "%s" u)


