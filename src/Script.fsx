#r @"..\packages\Hammock.1.2.3\lib\net40\Hammock.dll"
#r @"..\packages\TweetSharp.2.0.6\lib\4.0\Hammock.ClientProfile.dll"
#r @"..\packages\TweetSharp.2.0.6\lib\4.0\TweetSharp.dll"
#r @"..\packages\HtmlAgilityPack.1.4.0\lib\HtmlAgilityPack.dll"
#r @"..\packages\TweetSharp.2.0.6\lib\4.0\Newtonsoft.Json.dll"
#r @"..\csharp\web_snapshot\bin\Debug\SnappyBird.WebsiteSnapshotCSharpCheat.dll"


#load "TheInternet.fs"
#load "Twitter.fs"
#load "Output.fs"
#load "Bot.fs"

open System
open System.IO
open System.Collections.Generic
open System.Web
open System.Net
open TweetSharp
open HtmlAgilityPack
open Newtonsoft.Json.Linq
open SnappyBird



// printing short cuts
let print_trend = Twitter.print_trend
let print_tweet = Twitter.print_tweet


// get whats relevant, hot, and ultimately pointless
printfn "getting trends"
let toptweets = Twitter.get_top_tweets()
let current_trends = Twitter.service.ListCurrentTrends()
let daily_trends = Twitter.service.ListDailyTrends()
let weekly_trends = Twitter.service.ListWeeklyTrends()



// my terms.
// this could be @usernames, or #hashtags or random query terms
let searchbox = @"
@gruber
@kottke
@codinghorror
@theonion
@sportsguy33
@grantland33
@HackerNews
@hotdogsladies
@badbanana
@wired
"

//printfn "getting my dudes"
//let dudes_i_follow = Twitter.get_dudes_this_screen_name_is_following "aguerrera"
//let dudes_following_me = Twitter.get_dudes_following_this_screen_name "aguerrera"

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

// terms from the daily_trends (could use weekly or current ,whatever)
let trend_terms = daily_trends
                    |> Seq.map( fun t -> t.Name)
                    |> Seq.distinct
                    |> Seq.toList

// jam all the hash terms together
let h1 = trend_terms
            |> Seq.toList
            |> List.filter ( fun t -> t.StartsWith("#") ) 
let h2 = search_terms |> List.filter ( fun s -> s.StartsWith("#") ) 
let hash_tags = List.append h1 h2

// jam all the non screenname, non hash terms together
let q1 = trend_terms
            |> Seq.toList
            |> List.filter ( fun t -> not (t.StartsWith("#")) ) 
let q2 = search_terms |> List.filter ( fun s -> (not (s.StartsWith("#")) && not (s.StartsWith("@")))) 
let query_terms = List.append q1 q2

// let's get a lot of tweets
printfn "getting lots of tweets"
let tweets_from_screennames = Twitter.get_tweets_for_screennames(screen_names) |> Twitter.as_tweetable |> Seq.toList
let tweets_from_hashtags = Twitter.get_tweets_for_searchterms(hash_tags) |> Twitter.as_tweetable |> Seq.toList
let tweets_from_queryterms = Twitter.get_tweets_for_searchterms(query_terms) |> Twitter.as_tweetable |> Seq.toList

// filter out some of my tweets for this guy -------------v
//Twitter.print_via_screenname tweets_from_screennames "codinghorror"

// print out tweets from screennames
//Twitter.print_tweets_from_screen_names tweets_from_screennames screen_names

// get the tweets from this mysterious "toptweets" user
printfn "getting TOP TWEETS tweets"
let toptweets_tweets = Twitter.get_tweets_for_screenname("toptweets") |> Twitter.as_tweetable |> Seq.toList
//Twitter.print_via_screenname toptweets_tweets "toptweets"

let all_tweets = 
    tweets_from_screennames 
    |> Seq.append toptweets_tweets  // append the tweets from this mysterious toptweets user
    |> Seq.append tweets_from_hashtags  // append hashtag tweets. at your own risk. There are trends jammed in there (see above)
    |> Seq.append tweets_from_queryterms // append hashtag tweets. at your own risk. There are trends jammed in there (see above)
    |> Seq.toList


// who are the authors of all these tweets
let authors = 
        Twitter.get_authors_from_tweets(all_tweets)
        |> Seq.toList

printfn "our authors!"
authors |> Seq.sort |> Seq.iter (fun s -> printfn "%s" s)

// get some urls from the actual tweets
let url_map = Twitter.get_urls_from_tweets all_tweets

printfn "our authors and their urls!"
url_map
    |> List.iter ( fun (sn, u) -> printfn "%s\t%s" sn u)  // print out the screenname/url


// flatten authors and the urls map into a list of (author, [urls])
let authors_and_urls = 
    authors
    |> List.map (fun a ->
                    let urlxs = 
                            url_map
                            |> List.filter (fun (sn, u) -> (sn = a))
                            |> List.map (fun (_, u) -> u)
                    (a, urlxs)
                )

// this one is actually a little tricky.  it takes our authors_and_urls and 
// turns it into a map list of authors and their url infos.
// what's a url info? it's a tuple of fullurl, page title, status [1/0], and original [ie shortened] url
// i could put this async {} , but would it actually speed it up?  it's IO bound.
let get_author_and_url_info_async (sn, urls) =                 
        printfn "getting author and url info. Long op!"
        async {
            let infos = 
                    urls 
                    |> List.map (fun u ->
                                        printfn "getting %s: url: %s" sn u
                                        TheInternet.get_url_info(u)
                                        ) // warning!  IO bound action.
            return (sn, infos)
        }

// run the job asyncronously not sure if this even works
printfn "get authors and their infos.  asyncrhonously"
let author_and_url_infos =                 
    authors_and_urls
    //|> Seq.take 1
    |> Seq.toList
    |> Seq.map get_author_and_url_info_async
    |> Async.Parallel 
    |> Async.RunSynchronously
    |> Seq.toList

printfn "let's get the urls."
let my_urls = 
        let xs = seq { for sn, info in author_and_url_infos do
                            for (url,title,status,shorturl) in info do
                                if (status = 1) then yield url
                    }
        xs |> Seq.toList

//for hashtags/mentions/urls
let do_stuff_with_tweetents some_tweets = 
    // this does some filtering for tweets with entities (ie, hashtags/mentions/urls)
    let tweetents = Twitter.get_tweets_with_entities some_tweets |> Seq.toList
    let hts = tweetents |> Seq.collect ( fun t -> t.Entities.HashTags ) |> Seq.toList
    let mentions = tweetents |> Seq.collect ( fun t -> t.Entities.Mentions ) |> Seq.toList
    let urls = tweetents |> Seq.collect ( fun t -> t.Entities.Urls ) |> Seq.toList

    printfn "entities: url data and stuff"
    tweetents |> Seq.iter (fun t -> printfn "%s\n%s\n\n" t.Author.ScreenName t.Text)
    hts |> Seq.iter (fun h -> printfn "%s" h.Text)
    urls |> Seq.iter (fun u -> printfn "%s" u.Value)
    mentions |> Seq.iter (fun m -> printfn "%A\t=>\t%s\n" m.Id m.ScreenName)
    ()
//do_stuff_with_tweetents all_tweets



printfn "ready!"

// our dumping grounds.  no, you probably don't have this folder on your computer.
let output_dir = @"c:\staging\snappy_output\"


let get_web_thumb(url) =                 
        let fn = Output.get_hash_string_for_obj(url)
        let clean_url = Twitter.clean_twitter_url url
        let ext = ".jpg"
        let path = output_dir + fn + ext
        printfn "getting thumb for %s" clean_url
        printfn "   saving to %s" path
        TheInternet.get_website_bitmap_and_save path clean_url 1200 1200 700 700  
        ()

// lets get print out some thumbnails
// Waring: IO Bound!  this is slow!!! 
// could use some async/parallization if it is being used as part of another set of instructions.
printfn "dumping those thumbnails.  Long op!"
my_urls
    |> Seq.toList
    |> Seq.iter (fun u -> get_web_thumb u) // <- this is the sync version


// PRINT OUT ALL OF OUR STUFF.
let print_our_stuff = 
    printfn "top tweets"
    toptweets |> Seq.iter (fun t -> print_tweet t)

    printfn "daily trends"
    daily_trends |> Seq.iter print_trend

    printfn "current trends"
    current_trends |> Seq.iter print_trend

    printfn "weekly trends"
    weekly_trends |> Seq.iter print_trend

    printfn "our authors!"
    authors |> Seq.sort |> Seq.iter (fun s -> printfn "%s" s)

    printfn "our authors and their urls!"
    url_map
        |> List.iter ( fun (sn, u) -> printfn "%s\t%s" sn u)  // print out the screenname/url

    printfn "author and url infos.  got to be a better way to print this"
    author_and_url_infos
            |> Seq.iter (fun (sn, infos) -> 
                                infos |> 
                                    List.iter (fun (u, t, s, _) -> printfn "%s\t%s\t%s" sn u t)
                                ()
                          )  

