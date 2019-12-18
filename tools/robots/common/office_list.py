from bs4 import BeautifulSoup
import json

from download import download_html_with_urllib


def read_one_office_info (table_url):
    html, info = download_html_with_urllib(table_url)
    soup = BeautifulSoup(html, 'html5lib')
    office_info = {};
    for text in soup.findAll('div', {"class": "text"}):
        for table in text.findChildren('table', recursive=True):
            for row in table.findChildren('tr'):
                if row.text.find('Web-адрес') != -1:
                    cells = list(row.findAll('td'))
                    office_info['url'] = cells[1].text
    return office_info


def write_offices(offices):
    with open("offices.txt", "w", encoding="utf8") as outf:
        outf.write(json.dumps(offices, ensure_ascii=False,indent=4))


def read_office_list():
    with open("offices.txt", "r", encoding="utf8") as inpf:
        return json.loads(inpf.read())


def create_office_list():
    html, info = download_html_with_urllib("https://www.consultant.ru/document/cons_doc_LAW_65443/")
    soup = BeautifulSoup(html, 'html5lib')
    offices = []
    for  l in soup.findAll('a'):
        words = l.text.split()
        if len(words) == 0:
            continue
        first_word = words[0]
        if first_word not in {u"Министерство", u"Федеральное", u"Федеральная", u"Главное", u"Управление", u"Государственная", u"Служба"}:
            continue
        url = l.attrs['href']
        if not url.startswith('http://www.consultant.ru'):
            url = 'http://www.consultant.ru' + url
        office_info = read_one_office_info(url)
        office_info['name'] = l.text.strip('\n ')
        offices.append(office_info)
    write_offices(offices)
    return offices
