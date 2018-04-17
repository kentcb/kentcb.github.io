---
layout: you-i-and-reactiveui
title: You, I, and ReactiveUI
permalink: you-i-and-reactiveui/
categories: reactiveui book
---

THIS IS A DRAFT

# You, I, and ReactiveUI

## Write complex user interfaces, but without the code complexity

<div style="text-align: center">
<img src="cover.png" style="border-radius: 0px"/>
</div>

* Have you ever chased a bug for hours, only to discover the root cause is an innocuous looking piece of state?
* Do you ever wonder whether there's a sane, elegant way to write code that ties together wildly disparate asynchronous data sources?
* Have you ever guiltily omitted unit tests for time-sensitive code, simply because it was too hard to test?
* Do you ever dream of writing the vast majority of your UI application code once, and then using that code to target totally unrelated platforms?

<!-- Book preview slideshow -->
<div class="book-slideshow-container">
  <div class="bookPreviewSlides fade">
    <div class="numbertext">1 / 5</div>
    <img src="book_preview1.png" style="width:100%">
    <div class="text">Preview 1</div>
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">2 / 5</div>
    <img src="book_preview2.png" style="width:100%">
    <div class="text">Preview 2</div>
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">3 / 5</div>
    <img src="book_preview3.png" style="width:100%">
    <div class="text">Preview 3</div>
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">4 / 5</div>
    <img src="book_preview4.png" style="width:100%">
    <div class="text">Preview 4</div>
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">5 / 5</div>
    <img src="book_preview5.png" style="width:100%">
    <div class="text">Preview 5</div>
  </div>
</div>

*You, I, and ReactiveUI* is a comprehensive and enlightening book that will teach you how ReactiveUI can help solve all these problems and more. Beautifully typeset, and accompanied by an incredible set of code samples, it walks you through all of ReactiveUI's major features, describing how each can be used to level-up your .NET UI applications.

<!-- Book preview slideshow -->
<div class="samples-slideshow-container">
  <div class="samplePreviewSlides fade">
    <div class="numbertext">1 / 5</div>
    <img src="sample_preview1.png" style="width:100%">
    <div class="text">Preview 1</div>
  </div>
  <div class="samplePreviewSlides fade">
    <div class="numbertext">2 / 5</div>
    <img src="sample_preview2.png" style="width:100%">
    <div class="text">Preview 2</div>
  </div>
  <div class="samplePreviewSlides fade">
    <div class="numbertext">3 / 5</div>
    <img src="sample_preview3.png" style="width:100%">
    <div class="text">Preview 3</div>
  </div>
  <div class="samplePreviewSlides fade">
    <div class="numbertext">4 / 5</div>
    <img src="sample_preview4.png" style="width:100%">
    <div class="text">Preview 4</div>
  </div>
  <div class="samplePreviewSlides fade">
    <div class="numbertext">5 / 5</div>
    <img src="sample_preview5.png" style="width:100%">
    <div class="text">Preview 5</div>
  </div>
</div>

Stop managing application state like a dinosaur and unleash the power of reactive programming on your user interfaces.

BIG OL' BUTTON TO BUY THE BOOK (link to blurb.com)

<script>
var slideIndices = [0,0];

function autoShowSlides(className, indiceIndex) {
    var i;
    var slides = document.getElementsByClassName(className);
    for (i = 0; i < slides.length; i++) {
        slides[i].style.display = "none";
    }
    slideIndices[indiceIndex] += 1;
    if (slideIndices[indiceIndex] > slides.length) {slideIndices[indiceIndex] = 1}
    var index = slideIndices[indiceIndex];
    slides[index-1].style.display = "block";
    setTimeout(function() {
        autoShowSlides(className, indiceIndex);
    }, 5000);
}

autoShowSlides("bookPreviewSlides", 0);
autoShowSlides("samplePreviewSlides", 1);
</script>