#!/usr/bin/env python
#
# Copyright 2007 Google Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#

__author__ = 'Vaughan Rouesnel'

import datetime
import os
import random
import string
import sys

from google.appengine.ext.webapp import util
import logging
from google.appengine.ext import webapp
from google.appengine.ext.webapp.mail_handlers import InboundMailHandler
from google.appengine.api import users
from google.appengine.ext import db
from google.appengine.ext.webapp import template

_DEBUG = True

class BaseRequestHandler(webapp.RequestHandler):
  """Supplies a common template generation function.

  When you call generate(), we augment the template variables supplied with
  the current user in the 'user' variable and the current webapp request
  in the 'request' variable.
  """

  def generate(self, template_name, template_values={}):
    values = {
      'request': self.request,
      'user': users.get_current_user(),
      'login_url': users.create_login_url(self.request.uri),
      'logout_url': users.create_logout_url('http://%s/' % (
        self.request.host,)),
      'debug': self.request.get('deb'),
      'application_name': 'HealthKick', }
    values.update(template_values)
    directory = os.path.dirname(__file__)
    path = os.path.join(directory, os.path.join('templates', template_name))
    self.response.out.write(template.render(path, values, debug=_DEBUG))


class LogSenderHandler(InboundMailHandler):
  def receive(self, mail_message):
    logging.info("Received a message from: " + mail_message.sender)


class MainHandler(BaseRequestHandler):
  def get(self):
    #self.response.out.write('Hello world!')
    self.generate('index.html')


class User(db.Model):
  name = db.UserProperty()


def main():
  application = webapp.WSGIApplication([
      ('/', MainHandler),
                        LogSenderHandler.mapping()], debug=True)
  util.run_wsgi_app(application)


if __name__ == '__main__':
  main()
