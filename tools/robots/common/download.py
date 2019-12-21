import ssl
import sys
import urllib.parse
import urllib.request
import json
import hashlib
import re
import requests
from urllib.parse import urlparse, quote, urlunparse

import os
from selenium import webdriver
import time
FILE_CACHE_FOLDER="cached"


def is_html_contents(info):
    content_type = info.get('Content-Type', "text").lower()
    return content_type.startswith('text')


def get_url_headers (url):
    return requests.head(url).headers


def download_with_urllib (url):
    mvd = "https://" + u'мвд.рф'.encode('idna').decode('latin')
    url = url.replace('http://www.mvd.ru', mvd)
    o = list(urlparse(url)[:])
    o[2] = quote(o[2])
    url = urlunparse(o)
    context = ssl._create_unverified_context()
    req = urllib.request.Request(
        url,
        data=None,
        headers={
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_9_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.1916.47 Safari/537.36'
        }
    )
    data = ''
    info = {}
    headers = None
    print ("\turlopen..")
    with urllib.request.urlopen(req, context=context, timeout=20.0) as request:
        print("\treaddata...")
        data = request.read()
        info = request.info()
        headers = request.headers
    try:
        if is_html_contents(info):
            print("\tencoding..")
            encoding = headers.get_content_charset()
            if encoding == None:
                match = re.search('charset=([^"\']+)', data.decode('latin', errors="ignore"))
                if match:
                    encoding = match.group(1)
                else:
                    raise ValueError('unable to find encoding')

            data = data.decode(encoding, errors="ignore")

    except AttributeError:
        pass

    return (data, info)


def read_cache_file(localfile, info_file):
    is_binary = False
    with open(info_file, "r", encoding="utf8") as inf:
        info = json.loads(inf.read())
        cached_headers = info['headers']
        is_binary = not is_html_contents(cached_headers)
    if is_binary:
        with open(localfile, "rb") as f:
            return f.read()
    else:
        with open(localfile, encoding="utf8") as f:
            return f.read()

def write_cache_file(localfile, info_file, info, data):
    if is_html_contents(info):
        with open(localfile, "w", encoding="utf8") as f:
            f.write(data)
    else:
        with open(localfile, "wb") as f:
            f.write(data)

    if info is not None:
        with open(info_file, "w", encoding="utf8") as f:
            headers_and_url = dict()
            if hasattr(info, "_headers"):
                headers_and_url['headers'] = dict(info._headers)
            else:
                headers_and_url['headers'] = dict()
            f.write(json.dumps(headers_and_url, indent=4, ensure_ascii=False))
    return data


def get_local_file_name_by_url(url):
    global FILE_CACHE_FOLDER
    if not os.path.exists(FILE_CACHE_FOLDER):
        os.mkdir(FILE_CACHE_FOLDER)

    localfile = url
    if localfile.startswith('http://'):
        localfile = localfile[7:]
    if localfile.startswith('https://'):
        localfile = localfile[8:]
    localfile = localfile.replace(':', '_')
    localfile = localfile.replace('/', '\\')
    localfile = localfile.replace('&', '_')
    localfile = localfile.replace('=', '_')
    localfile = localfile.replace('?', '_')
    if len(localfile) > 64:
        localfile = localfile[0:64] + "_" + hashlib.md5(url.encode('utf8',  errors="ignore")).hexdigest()
    localfile = os.path.join(FILE_CACHE_FOLDER, localfile)
    if not localfile.endswith('html') and not localfile.endswith('htm'):
        localfile += "/index.html"
    if not os.path.exists(os.path.dirname(localfile)):
        os.makedirs(os.path.dirname(localfile))
    return localfile


def download_with_cache(url):
    localfile = get_local_file_name_by_url(url)
    info_file = localfile + ".headers"
    if os.path.exists(localfile):
        return read_cache_file(localfile, info_file)
    else:
        data, info = download_with_urllib(url)
        if len(data) == 0:
            return ""
        write_cache_file(localfile, info_file, info, data)
        return data


def download_and_cache_with_selenium (url):
    browser = webdriver.Firefox()
    browser.minimize_window();
    browser.get(url)
    time.sleep(10)
    html = browser.page_source
    browser.close()
    browser.quit()
    return html





