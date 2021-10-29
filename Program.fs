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
    let mutable companyName = ""
    // NOM, CIR, QR
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
        // find NOM
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

                companyName <-
                    doc.CssSelect("a[title$='기업개황 새창']")
                    |> List.map (fun a -> a.InnerText().Trim())
                    |> List.item 0

                checkData.[0] <- true

        // find CIR
        if not checkData.[1] then
            let cirData =
                doc.CssSelect("a[title^=주주총회소집공고]")
                |> List.map (fun n -> n.InnerText(), n.AttributeValue("href"))

            if not cirData.IsEmpty then
                let cirToString = cirData.Item(0).ToString()

                let cirURLList =
                    cirToString.Split [| ',' |]
                    |> Array.toList
                    |> List.item (1)

                let trimCirURL = cirURLList.Trim() |> Seq.toList

                let finalResult =
                    trimCirURL.[0..trimCirURL.Length - 2]
                    |> List.toArray
                    |> System.String

                cirLink <- prefixURL + finalResult
                checkData.[1] <- true

        // find QR
        if not checkData.[2] then
            let qrData =
                doc.CssSelect("a[title^=분기보고서]")
                |> List.map (fun n -> n.InnerText(), n.AttributeValue("href"))

            if not qrData.IsEmpty then
                let qrToString = qrData.Item(0).ToString()

                let qrURLList =
                    qrToString.Split [| ',' |]
                    |> Array.toList
                    |> List.item (1)

                let trimQrURL = qrURLList.Trim() |> Seq.toList

                let finalResult =
                    trimQrURL.[0..trimQrURL.Length - 2]
                    |> List.toArray
                    |> System.String

                qrLink <- prefixURL + finalResult
                checkData.[2] <- true
            *)


    0
