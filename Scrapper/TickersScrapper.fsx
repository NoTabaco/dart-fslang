module TickersScrapper

#r "nuget: FSharp.Data, 4.2.4"

open FSharp.Data
open System.Text.RegularExpressions

type CsvType = CsvProvider<"Name,NOM Link,CIR Link,QR Link,Ticker", Separators=",", HasHeaders=true>

let scrape =
    let tickers =
        [ "005930"
          "138690"
          "006260"
          "093050"
          "004870" ]

    let prefixURL = "https://dart.fss.or.kr"

    let mutable rows = []

    for ticker in tickers do
        let mutable reqUrl =
            sprintf "https://dart.fss.or.kr/dsab001/searchCorp.ax?textCrpNm=%s&currentPage=1&maxResults=50" ticker

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

        // NOM, CIR, QR
        let mutable nomLink = ""
        let mutable cirLink = ""
        let mutable qrLink = ""
        let mutable checkData = [| false; false; false |]

        let companyNameList =
            doc.CssSelect("a[onclick^='openCorpInfoNew']")
            |> List.map (fun a -> a.InnerText().Trim(), a.AttributeValue("onclick"))
            |> List.item 0

        let _, formValueString = companyNameList

        let formValueList =
            formValueString.Split [| '(' |]
            |> Array.toList
            |> List.item 1
            |> Seq.toList

        let formValue =
            formValueList.[1..8]
            |> List.toArray
            |> System.String

        let companyInfoUrl =
            sprintf "https://dart.fss.or.kr/dsae001/selectPopup.ax?selectKey=%s" formValue

        let companyInfoDoc = HtmlDocument.Load(companyInfoUrl)

        let filterCompany text =
            Regex.IsMatch(text, @"(?:[a-zA-Z0-9.,]+$)")

        let companyName =
            companyInfoDoc.CssSelect("td")
            |> List.map (fun a -> a.InnerText().Trim())
            |> List.filter (fun (title) -> filterCompany (title))
            |> List.item 1

        seq {
            for currentPage in 1 .. lastPage do
                printfn "Scrapping Dart Company '%s': Page %i" companyName currentPage

                if currentPage <> 1 then
                    reqUrl <-
                        sprintf
                            "https://dart.fss.or.kr/dsab001/searchCorp.ax?textCrpNm=%s&currentPage=%i&maxResults=50"
                            ticker
                            currentPage

                    doc <- HtmlDocument.Load(reqUrl)

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
                        checkData.[0] <- true
                    elif currentPage = lastPage && not checkData.[0] then
                        nomLink <- "Not Found"

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
                    elif currentPage = lastPage && not checkData.[1] then
                        cirLink <- "Not Found"

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
                    elif currentPage = lastPage && not checkData.[2] then
                        qrLink <- "Not Found"

                let canSkip =
                    checkData.[0] && checkData.[1] && checkData.[2]

                if currentPage = lastPage || canSkip then
                    rows <-
                        [ CsvType.Row(companyName, nomLink, cirLink, qrLink, ticker) ]
                        |> List.append rows

                    printfn "Finished Dart Company '%s': Page %i" companyName currentPage
                    yield ()
        }
        |> Seq.tryItem 0
        |> ignore

    printfn "Making Csv File..."
    let csv = CsvType.GetSample().Truncate(0)
    let csvFile = csv.Append rows
    csvFile.Save("./ticker_companies.csv")
    printfn "Completed !"
