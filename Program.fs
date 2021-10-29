// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open FSharp.Data
type CsvType = CsvProvider<"Name,NomLink,CirLink,3QRLink,Ticker", Separators=",", HasHeaders=false>

[<EntryPoint>]
let main argv =
    // Scrapper.scrape
    let tickers = [ "078600" ]
    let prefixURL = "https://dart.fss.or.kr"

    let mutable reqUrl =
        "https://dart.fss.or.kr/dsab001/searchCorp.ax?textCrpNm=005930&currentPage=1&maxResults=50"

    let mutable doc = HtmlDocument.Load(reqUrl)

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
            lastPageList.[1] |> string |> int
        with
        | :? System.FormatException -> lastPageList.[0] |> string |> int
        | _ -> lastPageList.[0..1] |> string |> int

    let mutable rows = []
    // NOM, CIR, 3QR
    let mutable nomLink = ""
    let mutable cirLink = ""
    let mutable qrLink = ""
    let mutable checkData = [| false; false; false |]

    for currentPage in 1 .. lastPage do
        if currentPage <> 1 then
            reqUrl <-
                sprintf
                    "https://dart.fss.or.kr/dsab001/searchCorp.ax?textCrpNm=005930&currentPage=%i&maxResults=50"
                    currentPage

            doc <- HtmlDocument.Load(reqUrl)
        (*
        // find Nom
        if not checkData.[0] then
            let nomData =
                doc.CssSelect("a[title^=주주총회소집결의]")
                |> List.map (fun n -> n.InnerText(), n.AttributeValue("href"))

            if not nomData.IsEmpty then
                let nomToString = nomData.Item(0).ToString()

                let nomURLList =
                    nomToString.Split [| ',' |]
                    |> Array.toList
                    |> List.item (1)

                let trimNomURL = nomURLList.Trim() |> Seq.toList

                let finalResult =
                    trimNomURL.[0..trimNomURL.Length - 2]
                    |> List.toArray
                    |> System.String

                nomLink <- prefixURL + finalResult
                checkData.[0] <- true
        *)
        if not checkData.[0] then printfn "1"



    0
