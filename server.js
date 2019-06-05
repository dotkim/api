require('dotenv').config();
const http = require('http');
const express = require('express');
const dateString = require('./components/dateString.js');
const jsonParser = require('body-parser').json({ type: 'application/json' });

console.log('############### WEB SERVER START UP ###############');
console.log(dateString(), '- starting http server');

const port = process.env.HTTPPORT || 80;

const app = new express();
app.disable('x-powered-by');
app.use(jsonParser);

// Global logging
app.use(function(req, res, next) {
  let start = Date.now();
  res.setHeader('Access-Control-Allow-Origin', '*');                                        // Website you wish to allow to connect
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS, PUT, PATCH, DELETE');  // Request methods you wish to allow
  res.setHeader('Access-Control-Allow-Headers', '*');                                       // Request headers you wish to allow
  res.setHeader('Access-Control-Allow-Credentials', false);                                 // Set to true if you need the website to include cookies
  
  res.on('finish', function() {
    var code = res._header ? String(res.statusCode) : String(-1);
    var duration = Date.now() - start;
    console.log(dateString(), '-', req.method, req.originalUrl, duration, code);
  });

  // Pass to next layer of middleware
  next();
});

app.use(express.static('public'));
app.use(express.static(process.env.IMGPATHSTATIC, require('./routes/static.js')));

app.use('/images', require('./routes/images.js'));
app.use('/insert', require('./routes/insert.js'));

http.createServer(app).listen(port);
console.log(dateString(), '- listening on port', port);