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
#load "PuttingItTogether.fs"

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

let screen_names, hash_tags, query_terms = PuttingItTogether.parse_terms searchbox

// let's get a lot of tweets
printfn "getting lots of tweets"
let tweets_from_screennames = Twitter.get_tweets_for_screennames(screen_names) |> Twitter.as_tweetable |> Seq.toList
let tweets_from_hashtags = Twitter.get_tweets_for_searchterms(hash_tags) |> Twitter.as_tweetable |> Seq.toList
let tweets_from_queryterms = Twitter.get_tweets_for_searchterms(query_terms) |> Twitter.as_tweetable |> Seq.toList

printfn "getting my dudes"
//let dudes_following_me = Twitter.get_dudes_following_this_screen_name "aguerrera"
let dudes_i_follow = Twitter.get_dudes_this_screen_name_is_following "aguerrera"
let tweets_from_dudes_i_follow = Twitter.get_tweets_for_screennames(dudes_i_follow) |> Twitter.as_tweetable |> Seq.toList

// get the tweets from this mysterious "toptweets" user
printfn "getting TOP TWEETS tweets"
let toptweets_tweets = Twitter.get_top_tweets() 


let all_tweets = PuttingItTogether.merge_all_tweets [tweets_from_screennames; toptweets_tweets; tweets_from_hashtags; tweets_from_queryterms; tweets_from_dudes_i_follow]

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
let authors_and_urls = PuttingItTogether.map_authors_to_urls authors url_map

let author_and_url_infos = PuttingItTogether.get_author_url_infos authors_and_urls

printfn "let's get the urls."
let my_urls = PuttingItTogether.get_distinct_urls_from_infos author_and_url_infos


//for hashtags/mentions/urls

printfn "ready!"

// our dumping grounds.  no, you probably don't have this folder on your computer.
// Waring: IO Bound!  this is slow!!! 
printfn "dumping those thumbnails.  Long op!"
let output_dir = @"c:\staging\snappy_output\"
PuttingItTogether.get_thumbs my_urls output_dir

