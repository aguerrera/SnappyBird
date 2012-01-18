#r @"..\packages\Hammock.1.2.3\lib\net40\Hammock.dll"
#r @"..\packages\TweetSharp.2.0.6\lib\4.0\Hammock.ClientProfile.dll"
#r @"..\packages\TweetSharp.2.0.6\lib\4.0\TweetSharp.dll"
#r @"..\packages\HtmlAgilityPack.1.4.0\lib\HtmlAgilityPack.dll"
#r @"..\packages\TweetSharp.2.0.6\lib\4.0\Newtonsoft.Json.dll"

#load "TheInternet.fs"
#load "Twitter.fs"

open System
open System.IO
open System.Collections.Generic
open System.Web
open System.Net
open TweetSharp
open HtmlAgilityPack
open Newtonsoft.Json.Linq
open SnappyBird


(*
 - output to static html and csv files 
 - get snapshot of link (or get image)
 - pre populate with toptweets trends, hastags, or own friends, someone else's view, or other sources
 - snappybirdbot
 - use FsCharting for chart output
    http://blogs.msdn.com/b/fsharpteam/archive/2011/04/15/getting-started-with-fsharpchart.aspx
 - get friends of friends of friends -> subequent generations
 - determine url quality
 - do stock lookups

.. semantics
http://www.alchemyapi.com/tools/
http://text-processing.com/docs/
http://developer.zemanta.com/

f#charting

*)



let print_trend = Twitter.print_trend
let print_tweet = Twitter.print_tweet



let toptweets = Twitter.get_top_tweets()
toptweets |> Seq.iter (fun t -> print_tweet t)

let current_trends = Twitter.service.ListCurrentTrends()
current_trends |> Seq.iter print_trend

let daily_trends = Twitter.service.ListDailyTrends()
daily_trends |> Seq.iter print_trend

let weekly_trends = Twitter.service.ListWeeklyTrends()
weekly_trends |> Seq.iter print_trend

let searchbox = @"
@gruber
@kottke
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


let tweets_from_screennames = Twitter.get_tweets_for_screennames(screen_names) |> Twitter.as_tweetable |> Seq.toList
let tweets_from_hashtags = Twitter.get_tweets_for_searchterms(hash_tags) |> Twitter.as_tweetable |> Seq.toList
let tweets_from_queryterms = Twitter.get_tweets_for_searchterms(query_terms) |> Twitter.as_tweetable |> Seq.toList

Twitter.print_via_screenname tweets_from_screennames "codinghorror"
Twitter.print_tweets_from_screen_names tweets_from_screennames screen_names

let xs = Twitter.get_tweets_for_screenname("toptweets") |> Twitter.as_tweetable |> Seq.toList
Twitter.print_via_screenname xs "toptweets"


let all_tweets = 
    tweets_from_screennames
    |> Seq.append xs 
//    |> Seq.append tweets_from_hashtags
//    |> Seq.append tweets_from_queryterms
    |> Seq.toList



let authors = 
        Twitter.get_authors_from_tweets(all_tweets)

authors |> Seq.sort |> Seq.iter (fun s -> printfn "%s" s)

let url_map = Twitter.get_urls_from_tweets all_tweets
url_map
    |> List.iter ( fun (sn, u) -> printfn "%s\t%s" sn u)  // print out the screenname/url

let authors_and_urls = 
    authors
    |> Seq.map (fun a ->
                    let infos = 
                            url_map
                            |> List.filter (fun (sn, u) -> (sn = a))
                            |> List.map (fun (_, u) -> u)
                    (a, infos)
                )

let author_and_url_infos =                 
    authors_and_urls
        |> Seq.map ( fun (sn, urls) -> 
                            let infos = 
                                    urls 
                                    |> List.map (fun u -> TheInternet.get_url_info(u))
                            (sn, infos)
                    )  

author_and_url_infos
        |> Seq.iter (fun (sn, infos) -> 
                            infos |> 
                                List.iter (fun (u, t, s, _) -> printfn "%s\t%s\t%s" sn u t)
                            ()
                      )  

let tweetents = Twitter.get_tweets_with_entities all_tweets

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
                |> Seq.map (fun u -> TheInternet.get_url_info(u))

urlinfos |> Seq.iter (fun (u, t, s, o) -> printfn "%s\t%s\t%A" u t s)

let rawurls = urls 
                |> Seq.map (fun u -> u.Value)
                |> Seq.map (fun u -> TheInternet.get_uri(u))
rawurls |> Seq.iter (fun u -> printfn "%s" u)

let rawurl_list = rawurls |> Seq.toList


let output_dir = @"c:\staging\snappy_output\"

let get_thumb url = 
    let fn = output_dir + Guid.NewGuid().ToString("N") + ".jpg"
    printfn "getting thumb for %s" url
    printfn "   saving to %s" fn
    TheInternet.get_website_bitmap_and_save fn url 800 800
    ()

rawurl_list
    |> Seq.take 10
    |> Seq.toList
    |> List.iter (fun u -> get_thumb u)

