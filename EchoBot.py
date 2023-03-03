# -*- coding: utf-8 -*-
"""
Created on Tue Feb 21 11:26:24 2023

@author: EH_LIN
"""

from __future__ import unicode_literals
import os
from flask import Flask, request, abort,render_template
from linebot import LineBotApi, WebhookHandler
from linebot.exceptions import InvalidSignatureError
from linebot.models import MessageEvent, TextMessage, TextSendMessage,StickerSendMessage
#from http.server import HTTPServer, CGIHTTPRequestHandler

import configparser

import random

app = Flask(__name__)

# LINE 聊天機器人的基本資料
config = configparser.ConfigParser()
config.read('config.ini')  #從ini檔讀取 token 跟 secret

#channel_access_token = 'KM1UYHNieMA5d5hHqVKAClmyaEOQZmS6af3lUXucYdQkxJLsdMh06c12r5+eo/uzekR0gjQugn6r2s0AsUzZ9a1KffvkNY7jC1BZMPQmDw/Mw1bnkpANskqe6P7vBZfpCt5oChKxpMeYonK5DCAknQdB04t89/1O/w1cDnyilFU='
#channel_secret = '991c8a315d726bee6ca27b8cd4844d5f'

channel_access_token = config['Line']['channel_access_token']
channel_secret = config['Line']['channel_secret']


line_bot_api = LineBotApi(channel_access_token)
handler = WebhookHandler(channel_secret)



# 接收 LINE 的資訊
@app.route("/callback", methods=['POST'])
def callback():
    signature = request.headers['X-Line-Signature']

    body = request.get_data(as_text=True)
    app.logger.info("Request body: " + body)
    
    try:
        print(body, signature)
        handler.handle(body, signature)
        
    except InvalidSignatureError:
        abort(400)

    return 'OK'

# 學你說話
@handler.add(MessageEvent, message=TextMessage)
def pretty_echo(event):
    
    if event.source.user_id != "Udeadbeefdeadbeefdeadbeefdeadbeef":
        
        # Phoebe 愛唱歌
        pretty_note = '~'
        pretty_text = ''
        
        #for i in event.message.text:
        
        #    pretty_text += i
        #    pretty_text += random.choice(pretty_note)
        
        pretty_text = event.message.text
        if event.message.text == "@說明" :
            pretty_text = "指令1:@貼圖"
            line_bot_api.reply_message(event.reply_token,TextSendMessage(text=pretty_text)) 
               
        elif event.message.text == "1" :
            pretty_text = "測試事件1"
            line_bot_api.reply_message(event.reply_token,TextSendMessage(text=pretty_text)) 
        elif event.message.text == "@貼圖" :   
            pretty_text = StickerSendMessage(
                package_id = "8525",
                sticker_id= "16581297"    
            )
            line_bot_api.reply_message(event.reply_token,pretty_text) 
        else :
            pretty_text = "若要查詢指令，請輸入@說明"
            line_bot_api.reply_message(event.reply_token,TextSendMessage(text=pretty_text)) 
            

@app.route('/login', methods=['GET', 'POST']) 
def login():
    #  利用request取得使用者端傳來的方法為何
    if request.method == 'POST': 
                            #  利用request取得表單欄位值
        return 'Hello ' + request.values['username'] 
    #  非POST的時候就會回傳一個空白的模板
    return render_template('login.html')

        
@app.route("/")
def hello():
    return render_template('abc.html')
        

if __name__ == "__main__":
    app.run()