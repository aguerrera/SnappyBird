module SnappyBird.Output

    open System
    open System.Collections.Generic;
    open System.IO
    open HtmlAgilityPack

    // gets a stringified hashcode.  
    // appends a "1" if negative, a "zero" if >= 0
    let get_hash_string_for_obj obj = 
        let hc = obj.GetHashCode()
        if hc < 0 then
            Math.Abs(hc).ToString() + "1"
        else
            hc.ToString() + "0"

    let build_index = 
        // write last write date
        ()
    let write_tweet_sheet = 
        // write query & date to html
        // get tweets, authors, links
        // get relevance via reddit, hackernews
        // get profile images
        // get thumbnails
        // build html
        // store it as /y/m/d/s_termhash
        // build index
        ()