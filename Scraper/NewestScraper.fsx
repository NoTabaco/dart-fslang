module NewestScraper

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

            let mutable companyDataArray = []
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

                    companyDataArray <- List.append companyDataArray [ engCompanyName ]

            // NOM, CIR LINK
            if not companyDataArray.IsEmpty then
                let links =
                    results.CssSelect("a[title^='주주총회소집']")
                    |> List.map (fun n -> n.InnerText(), n.AttributeValue("href"))

                let tempLength = companyDataArray.Length - 1

                for i in 0 .. tempLength do
                    let mutable name, link = links.[i]
                    link <- prefixURL + link
                    name <- name.Trim()

                    rows <-
                        [ CsvType.Row(companyDataArray.[i], name, link) ]
                        |> List.append rows

    let mutable rowLength = rows.Length - 1
    // Delete duplicate
    printfn "Checking Duplicate Data..."

    for i = 0 to rowLength do
        if i <= rowLength then
            let company = rows.[i].Column1
            let name = rows.[i].Column2

            if name.Contains("[기재정정]") then
                let splitName =
                    name.Split [| ']' |]
                    |> Array.toList
                    |> List.item 1

                let mutable jIndex = i + 1
                // Max Index, Execute only if not Last Row
                if jIndex <> rows.Length then
                    for j = rowLength downto jIndex do
                        let checkingName = rows.[j].Column2

                        if
                            company = rows.[j].Column1
                            && splitName.Contains(checkingName)
                        then
                            rows <- rows |> List.filter ((<>) rows.[j])
                            rowLength <- rows.Length - 1
                            jIndex <- i - 1

    printfn "Making Csv File..."
    let csv = CsvType.GetSample().Truncate(0)
    let csvFile = csv.Append rows
    csvFile.Save("./company.csv")
    printfn "Completed !"
