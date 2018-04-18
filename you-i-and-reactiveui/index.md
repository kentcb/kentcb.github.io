---
layout: you-i-and-reactiveui
title: You, I, and ReactiveUI
permalink: you-i-and-reactiveui/
categories: reactiveui book
---

<div class="underlay">
<p class="underlay-text">THIS IS A DRAFT</p>
</div>

<div class="content">
<div class="contenttext">
<div class="title">
You, I, and ReactiveUI
</div>
<div class="subtitle">
Complex user interfaces, without code complexity
</div>

<div class="copy">
<ul class="isthisyou">
<li>Have you ever chased a bug for hours, only to discover the root cause is an innocuous looking piece of state?</li>
<li>Do you ever wonder whether there's a sane, elegant way to write code that ties together wildly disparate asynchronous data sources?</li>
<li>Have you ever guiltily omitted unit tests for time-sensitive code, simply because it was too hard to test?</li>
<li>Do you ever dream of writing the vast majority of your UI application code once, and then using that code to target totally unrelated platforms?</li>
</ul>

You, I, and ReactiveUI* is a comprehensive and enlightening book that will teach you how ReactiveUI can help solve all these problems and more. Beautifully typeset, and accompanied by an incredible set of code samples, it walks you through all of ReactiveUI's major features, describing how each can be used to level-up your .NET UI applications.
</div>

<!-- Book preview slideshow -->
<div class="book-slideshow-container">
  <div class="bookPreviewSlides fade">
    <div class="numbertext">1 / 10</div>
    <img src="book_preview1.png" style="width:100%">
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">2 / 10</div>
    <img src="sample_preview1.png" style="width:100%">
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">3 / 10</div>
    <img src="book_preview2.png" style="width:100%">
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">4 / 10</div>
    <img src="sample_preview2.png" style="width:100%">
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">5 / 10</div>
    <img src="book_preview3.png" style="width:100%">
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">6 / 10</div>
    <img src="sample_preview3.png" style="width:100%">
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">7 / 10</div>
    <img src="book_preview4.png" style="width:100%">
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">8 / 10</div>
    <img src="sample_preview4.png" style="width:100%">
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">9 / 10</div>
    <img src="book_preview5.png" style="width:100%">
  </div>
  <div class="bookPreviewSlides fade">
    <div class="numbertext">10 / 10</div>
    <img src="sample_preview5.png" style="width:100%">
  </div>
</div>

<div class="copy">
Stop managing application state like a dinosaur and unleash the power of reactive programming on your user interfaces.
</div>
<div class="buybuttoncontainer">
<button type="submit" name="buy" class="buybutton" data-label="BUY THIS BOOK">BUY THIS BOOK</button>
</div>
</div>

<div class="contentimages">
<div class="cover" style="text-align: center">
<img src="cover.png" style="border-radius: 0px"/>
</div>
</div>

</div>

<script>
var slideIndices = [0];

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
</script>