# Praca dyplomowa
### Tytuł: *Projekt i implementacja systemu wymiany informacji wykorzystującego geolokalizację*
### Wersja demonstracyjna aplikacji internetowej: *https://pd.kwyzgol.com/*
### Obrazy w repozytorium *Docker Hub*: *https://hub.docker.com/r/kwyzgol/pd-webapp/tags*

## Opis
<p align="justify">
Celem pracy było zaprojektowanie i zaimplementowanie systemu wymiany informacji wykorzystującego geolokalizację. Na utworzony system składa się aplikacja internetowa, aplikacja mobilna, relacyjna baza danych <i>MySQL</i> oraz nierelacyjna baza danych <i>Neo4j</i>.
</p>

<p align="justify">
Korzystając z aplikacji internetowej użytkownicy mogą przekazywać sobie informacje w postaci postów, komentarzy lub wiadomości prywatnych, a dzięki zaimplementowanemu systemowi słów kluczowych są w stanie łączyć się w grupy o podobnych zainteresowaniach. W kwestii wyświetlania postów system oferuje szerokie i intuicyjne możliwości filtrowania ze względu na lokalizację – pozwala na łatwe wyszukiwanie treści w promieniu od 100 metrów do 500 kilometrów od wskazanego punktu. Sprawia to, że użytkownik, używając swojej geolokalizacji, może odnaleźć interesujące go treści związane z jego najbliższym otoczeniem. Aplikacja mobilna służy wyłącznie do wysyłania kodów weryfikacji dwuetapowej poprzez wiadomości <i>SMS</i>.
</p>

<p align="justify">
Do wykonania obu aplikacji została wykorzystana platforma <i>.NET</i> oraz rozszerzająca ją technologia <i>Blazor</i>. Większość kodu źródłowego została napisana w języku <i>C#</i>, uzupełnionym o wstawki języka <i>HTML</i>.
</p>

### Wykonał Kamil Wyżgoł