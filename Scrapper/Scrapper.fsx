module NewestScrapper

#r "nuget: FSharp.Data, 4.2.4"

open FSharp.Data
open System
open System.Text.RegularExpressions

type CsvType = CsvProvider<"Name,NOM - CIR,NOM Link - CIR Link", Separators=",", HasHeaders=false>

let scrape =
    let mdayCnt =
        match (DateTime.Now).DayOfWeek with
        | DayOfWeek.Saturday -> 1
        | DayOfWeek.Sunday -> 2
        | _ -> 0

    let marketDatas =
        [ "Y", "Securities Market", mdayCnt
          "K", "KOSDAQ Market", mdayCnt
          "N", "KONEX Market", mdayCnt ]

    let prefixURL = "https://dart.fss.or.kr"
    let mutable rows = []

    for (pageGroup, marketName, pastDays) in marketDatas do
        let mutable reqUrl =
            sprintf
                "https://dart.fss.or.kr/dsac001/search.ax?pageGrouping=%s&currentPage=1&mdayCnt=%s"
                pageGroup
                (pastDays |> string)

        let mutable results = HtmlDocument.Load(reqUrl)

        let pageString =
            results.CssSelect("div[class=pageInfo]")
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
            | :? FormatException -> lastPageList.[0] |> string |> int
            | _ -> lastPageList.[0..1] |> string |> int

        let allResult =
            lastPageList.[4..lastPageList.Length - 2]
            |> List.toArray
            |> String

        let nowHeader =
            sprintf "%i.%i.%i" (DateTime.Now).Year (DateTime.Now).Month ((DateTime.Now).Day - pastDays)

        rows <-
            [ CsvType.Row(marketName, nowHeader, allResult) ]
            |> List.append rows

        for page in 1 .. lastPage do
            printfn "Scrapping Dart %s: Page %i" marketName page

            if page <> 1 then
                reqUrl <-
                    sprintf
                        "https://dart.fss.or.kr/dsac001/search.ax?pageGrouping=%s&currentPage=%s&mdayCnt=%s"
                        pageGroup
                        (page |> string)
                        (pastDays |> string)

                results <- HtmlDocument.Load(reqUrl)

            let allDatas =
                results.CssSelect("tr > td")
                |> List.map (fun x -> x.InnerText().Trim())

            // Find NOM, CIR
            for index in 0 .. 6 .. allDatas.Length - 1 do
                let listLines = allDatas.[index..index + 5]

                if
                    listLines.[2].Contains("주주총회소집결의")
                    || listLines.[2].Contains("주주총회소집공고")
                then
                    // ENG Name
                    let splitCompanyName = listLines.[1].Split [| ' ' |]

                    let findingCompany =
                        sprintf "a[title='%s 기업개황 새창']" splitCompanyName.[1]

                    let companyNameList =
                        results.CssSelect(findingCompany)
                        |> List.map (fun a -> a.InnerText().Trim(), a.AttributeValue("href"))
                        |> List.item 0

                    let _, formValueString = companyNameList

                    let formValueList =
                        formValueString.Split [| '(' |]
                        |> Array.toList
                        |> List.item 1
                        |> Seq.toList

                    let formValue =
                        formValueList.[1..8] |> List.toArray |> String

                    let companyInfoUrl =
                        sprintf "https://dart.fss.or.kr/dsae001/selectPopup.ax?selectKey=%s" formValue

                    let companyInfoDoc = HtmlDocument.Load(companyInfoUrl)

                    let filterCompany text =
                        Regex.IsMatch(text, @"(?:[a-zA-Z0-9.,]+$)")

                    let engCompanyName =
                        companyInfoDoc.CssSelect("td")
                        |> List.map (fun a -> a.InnerText().Trim())
                        |> List.filter (fun (title) -> filterCompany (title))
                        |> List.item 1

                    // NOM, CIR LINK
                    printfn "%s %s" engCompanyName listLines.[2]

            for (name, cir, cirLink) in [ "1", ",", "2" ] do
                rows <-
                    [ CsvType.Row(name, cir, cirLink) ]
                    |> List.append rows

    printfn "Making Csv File..."
    let csv = CsvType.GetSample().Truncate(0)
    let csvFile = csv.Append rows
    csvFile.Save("./company.csv")
    printfn "Completed !"
