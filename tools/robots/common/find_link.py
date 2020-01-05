import sys
import os
from pathlib import Path
import time
import logging
from bs4 import BeautifulSoup
import shutil
from urllib.parse import urljoin
from download import download_with_cache, OFFICE_FILE_EXTENSIONS, save_download_file
from content_types import  ALL_CONTENT_TYPES
from selenium import webdriver
from selenium.webdriver.firefox.options import Options as FirefoxOptions

class TLinkInfo:
    def __init__(self, text, source=None, target=None, tagName=None, download_file=None):
        self.Source = source
        self.Target = target
        self.Text = text
        self.TagName = tagName
        self.DownloadFile = download_file


def check_sub_page_or_iframe(link_info):
    if not check_self_link(link_info):
        return False
    if link_info.Target is None:
            return False
    if link_info.TagName is not None and link_info.TagName.lower() == "iframe":
        return True
    return link_info.Target.startswith(link_info.Source)




def check_self_link(link_info):
    if link_info.Target != None:
        if len(link_info.Target) == 0:
            return False
        if link_info.Target.find('redirect') != -1:
            return False
        if link_info.Source.strip('/') == link_info.Target.strip('/'):
            return False
    return True


def check_anticorr_link_text(link_info):
    if not check_self_link(link_info):
        return False

    text = link_info.Text.strip().lower()
    if text.startswith(u'противодействие'):
        return text.find("коррупц") != -1

    return False


def make_link(main_url, href):
    url = urljoin(main_url, href)
    # see http://minpromtorg.gov.ru/open_ministry/anti/activities/info/
    #i = url.find('#')
    #if i != -1:
    #    url = url[0:i]
    return url

class SomeOtherTextException(Exception):
    def __init__(self, value):
        self.value = value
    def __str__(self):
        return (repr(self.value))


def find_recursive_to_bottom (element, check_link_func):
    children = element.findChildren()
    if len(children) == 0:
        if len(element.text) > 0:
            if check_link_func(TLinkInfo(element.text)):
                return element.text
            if len (element.text.strip()) > 10:
                raise SomeOtherTextException (element.text.strip())
    else:
        for child in children:
            found_text = find_recursive_to_bottom(child, check_link_func)
            if len(found_text) > 0:
                return found_text
    return ""


def go_to_the_top (element, max_iterations_count, check_link_func):
    for i  in range(max_iterations_count):
        element = element.parent
        if element is None:
            return ""
        found_text = find_recursive_to_bottom (element, check_link_func)
        if len(found_text) > 0:
            return found_text
    return ""


def has_office_document_file_extension(href):
    global OFFICE_FILE_EXTENSIONS
    filename, file_extension = os.path.splitext(href)
    return file_extension.lower() in OFFICE_FILE_EXTENSIONS


def get_base_url(main_url, soup):
    for l in soup.findAll('base'):
        href = l.attrs.get('href')
        if href is not None:
            return href
    return main_url


def check_http(href):
    if href.startswith('mailto:'):
        return False
    if href.startswith('tel:'):
        return False
    return True


def find_links_in_html_by_text(main_url, html, check_link_func):
    soup = BeautifulSoup(html, 'html5lib')
    links = {}
    all_links_count = 0
    if has_office_document_file_extension(main_url):
        return links, all_links_count
    base = get_base_url(main_url, soup)
    logging.debug("find_links_in_html_by_text function={0}".format(check_link_func))
    for l in soup.findAll('a'):
        href = l.attrs.get('href')
        if href is not None:
            all_links_count += 1
            if not check_http(href):
                continue
            logging.debug("check link {0}".format(href))
            href = make_link(base, href)
            if  check_link_func( TLinkInfo(l.text, main_url, href, l.name) ):
                links[href] = {'text': l.text, 'engine': 'urllib', 'source':  main_url}
                logging.debug("link is good")
            else:
                if has_office_document_file_extension(href):
                    try:
                        found_text = go_to_the_top(l, 3, check_link_func)
                        if len(found_text) > 0:
                            links[href] = {'text': found_text, 'engine': 'urllib', 'source':  main_url}
                    except SomeOtherTextException as err:
                        continue

    for l in soup.findAll('iframe'):
        href = l.attrs.get('src')
        if href is not None:
            all_links_count += 1
            if not check_http(href):
                continue

            href = make_link(base, href)
            if check_link_func( TLinkInfo(l.text, main_url, href, l.name) ):
                links[href] = {'text': l.text, 'engine': 'urllib', 'source':  main_url}

    return links, all_links_count


TMP_DOWNLOAD_FOLDER = None
def recreate_tmp_download_folder():
    global TMP_DOWNLOAD_FOLDER
    TMP_DOWNLOAD_FOLDER = os.path.join(os.getcwd(), "tmp_download")
    if os.path.exists(TMP_DOWNLOAD_FOLDER):
        shutil.rmtree(TMP_DOWNLOAD_FOLDER)
    os.makedirs(TMP_DOWNLOAD_FOLDER)

def open_selenium():
    global TMP_DOWNLOAD_FOLDER
    recreate_tmp_download_folder()

    options = FirefoxOptions()
    options.set_preference("browser.download.folderList", 2)
    options.set_preference("browser.download.manager.showWhenStarting", False)
    options.set_preference("browser.download.manager.closeWhenDone", True)
    options.set_preference("browser.download.manager.focusWhenStarting", False)
    options.set_preference("browser.download.dir", TMP_DOWNLOAD_FOLDER)
    options.set_preference("browser.helperApps.neverAsk.saveToDisk", ALL_CONTENT_TYPES)
    options.set_preference("browser.helperApps.alwaysAsk.force", False)
    return webdriver.Firefox(firefox_options=options)

def wait_download_finished(timeout=120):
    global TMP_DOWNLOAD_FOLDER
    dl_wait = True
    seconds = 0
    while dl_wait and seconds < timeout:
        firefox_temp_file = sorted(Path(TMP_DOWNLOAD_FOLDER).glob('*.part'))
        chrome_temp_file = sorted(Path(TMP_DOWNLOAD_FOLDER).glob('*.crdownload'))
        if (len(firefox_temp_file) == 0) and \
           (len(chrome_temp_file) == 0):
            files = os.listdir(TMP_DOWNLOAD_FOLDER)
            if len(files) > 0:
                return save_download_file(os.path.join(TMP_DOWNLOAD_FOLDER, files[0]))
            return None
        time.sleep(1)
        seconds += 1
    return None


def find_links_with_selenium (main_url, check_link_func):
    links = dict()
    if has_office_document_file_extension(main_url):
        return links
    logging.debug("find_links_with_selenium url={0}, function={1}".format(main_url, check_link_func))
    driver = open_selenium()

    driver.get(main_url)
    time.sleep(6)
    elements = list(driver.find_elements_by_xpath('//button | //a'))

    for i in range(len(elements)):
        e = elements[i]
        tag_name = e.tag_name
        link_text = e.text.strip('\n\r\t ') #initialize here, can be broken after click
        logging.debug("check link url={0}, function={1}".format(main_url, check_link_func))
        if check_link_func(TLinkInfo(link_text)):
            recreate_tmp_download_folder()
            e.click()
            time.sleep(6)
            downloaded_file = wait_download_finished(120)
            link_url = driver.current_url
            if check_link_func(TLinkInfo(link_text, main_url, link_url, tag_name, downloaded_file)):
                record = {'text': link_text, 'engine': 'selenium', 'source':  main_url}
                if downloaded_file is not None:
                    record['downloaded_file'] = downloaded_file
                    link_url = "download:" + str(i)+ ":" + link_url
                links[link_url] = record
            driver.back()
            elements = list(driver.find_elements_by_xpath('//button | //a'))
    driver.quit()
    return links



def add_links(ad, url, check_link_func, fallback_to_selenium=True):
    html = ""
    try:
        html = download_with_cache(url)
    except Exception as err:
        logging.error('cannot download page url={0} while add_links, exception={1}\n'.format(url, str(err)))
        ad['exception'] = str(err)
        return

    try:
        links, all_links_count = find_links_in_html_by_text(url, html, check_link_func)

        # see http://minpromtorg.gov.ru/docs/#!svedeniya_o_dohodah_rashodah_ob_imushhestve_i_obyazatelstvah_imushhestvennogo_haraktera_federalnyh_gosudarstvennyh_grazhdanskih_sluzhashhih_minpromtorga_rossii_rukovodstvo_a_takzhe_ih_suprugi_supruga_i_nesovershennoletnih_detey_za_period_s_1_yanvarya_2018_g_po_31_dekabrya_2018_g
        if len(links) == 0 and fallback_to_selenium:
            links = find_links_with_selenium(url, check_link_func)
        if 'links' not in ad:
            ad['links'] = dict()
        ad['links'].update(links)

    except Exception as err:
        logging.error('cannot download page url={0} while find_links, exception={1}\n'.format(url, str(err)))
        ad['exception'] = str(err)



def find_links_in_page_with_urllib(url, check_link_func):
    try:
        html = download_with_cache(url)
        if html == "binary_data":
            return []
        links, _= find_links_in_html_by_text(url, html, check_link_func)
        return links
    except Exception as err:
        logging.error('cannot download page: ' + url + "\n")
        return []


FIXLIST =  {
    "anticorruption_div": [
        ('fsin.su', "http://www.fsin.su/anticorrup2014/"),
        ('fso.gov.ru',  "http://www.fso.gov.ru/korrup.html")
    ]
}


def find_links_for_one_website(start_pages, target, check_link_func, fallback_to_selenium=False, transitive=False):

    while True:
        save_count = len(target['links'])

        for url in start_pages:
            logging.info("find_links_for_one_website " + url + "\n")
            add_links(target, url, check_link_func, fallback_to_selenium)

        new_count = len(target['links'])
        if not transitive or save_count == new_count:
            break


def find_links_for_all_websites(offices, source_page_collection_name, target_page_collection_name,
                                check_link_func, fallback_to_selenium=True, transitive=False, only_missing=True,
                                include_source="copy_if_empty"):
    for office_info in offices:
        name = office_info['name']
        if target_page_collection_name not in office_info:
            office_info[target_page_collection_name] = dict()
        target = office_info[target_page_collection_name]
        if 'links' not in target:
            target['links'] = dict()

        if target.get('engine', '') == 'manual':
            logging.info("skip manual url updating {0}, target={1}\n".format(
                name, target_page_collection_name))
            continue
        if len(target['links']) > 0 and only_missing:
            logging.info("skip manual url updating {0}, target={1}, (already exist)\n".format(
                name, target_page_collection_name))
            continue

        start_pages = office_info.get(source_page_collection_name, {}).get('links', dict())

        if include_source == "always":
            target['links'].update(start_pages)

        find_links_for_one_website(start_pages, target,
                                   check_link_func, fallback_to_selenium, transitive)

        if include_source == "copy_if_empty" and len(target['links']) == 0:
            target['links'].update(start_pages)

        if len(target) == 0 and len(start_pages) > 0:
            for (s,t) in FIXLIST.get(target_page_collection_name, []):
                if start_pages[0].find(s) != -1:
                    target[t] = [{"text":  "", "engine": "manual"}]


def collect_subpages(offices, source_page_collection_name, target_page_collection_name, check_link_func,
                     include_source="always"):
    find_links_for_all_websites(offices, source_page_collection_name, target_page_collection_name,
                                check_link_func,
                                fallback_to_selenium=False,
                                transitive=True,
                                only_missing=False,
                                include_source=include_source)
