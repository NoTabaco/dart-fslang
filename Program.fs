// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open FSharp.Data

[<EntryPoint>]
let main argv =
    // Scrapper.scrape
    let tickers = [ "078600" ]

    let defaultUrl =
        "https://dart.fss.or.kr/dsab001/searchCorp.ax?textCrpNm=005930&currentPage=1&maxResults=50"

    let doc = HtmlDocument.Load(defaultUrl)

    let pageString =
        doc.CssSelect("div[class=pageInfo]")
        |> List.map (fun n -> n.InnerText())
        |> List.item 0

    let lastPageList =
        pageString.Split [| '/' |]
        |> Array.toList
        |> List.item 1
        |> Seq.toList

    let lastPage =
        try
            lastPageList |> string |> int
        with
        | :? System.FormatException -> lastPageList.[0] |> string |> int
        | _ -> lastPageList.[0..1] |> string |> int

    let t = lastPage
    printfn "%A" t


    0
